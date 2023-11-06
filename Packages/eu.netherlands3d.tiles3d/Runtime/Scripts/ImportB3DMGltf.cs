using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using UnityEngine.Networking;
using System.Threading.Tasks;
using GLTFast;
using System;
using SimpleJSON;
using System.Text;
using Newtonsoft.Json;
using Meshoptimizer;
using Unity.Collections;
using System.Linq;





#if UNITY_EDITOR
using System.IO.Compression;
#endif
namespace Netherlands3D.B3DM
{
    public class ImportB3DMGltf
    {
        private static CustomCertificateValidation customCertificateHandler = new CustomCertificateValidation();
        private static ImportSettings importSettings = new ImportSettings() { AnimationMethod = AnimationMethod.None };

        /// <summary>
        /// Helps bypassing expired certificate warnings.
        /// Use with caution, and only with servers you trust.
        /// </summary>
        public class CustomCertificateValidation : CertificateHandler
        {
            protected override bool ValidateCertificate(byte[] certificateData)
            {
                return true;
            }
        }

        /// <summary>
        /// Rerturns IENumerator for a webrequest for a .b3dm, .glb or .gltf and does a GltfImport callback on success
        /// </summary>
        /// <param name="url">Full url to .b3dm, .glb or .gltf file</param>
        /// <param name="callbackGltf">The callback to receive the GltfImport on success</param>
        /// <param name="webRequest">Provide </param>
        /// <param name="bypassCertificateValidation"></param>
        /// <returns></returns>
        public static IEnumerator ImportBinFromURL(string url, Action<ParsedGltf> callbackGltf, bool bypassCertificateValidation = false)
        {
            var webRequest = UnityWebRequest.Get(url);

            if (bypassCertificateValidation)
                webRequest.certificateHandler = customCertificateHandler; //Not safe; but solves breaking curl error

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning(url + " -> " + webRequest.error);
                callbackGltf.Invoke(null);
            }
            else
            {
                byte[] bytes = webRequest.downloadHandler.data;
                var memory = new ReadOnlyMemory<byte>(bytes);

                double[] rtcCenter = null;

                if (url.Contains(".b3dm"))
                {
                    var memoryStream = new MemoryStream(bytes);
                    var b3dm = B3dmReader.ReadB3dm(memoryStream);

                    //Optional RTC_CENTER from b3DM header
                    rtcCenter = GetRTCCenter(rtcCenter, b3dm);
                    //TODO: Get subobjects from b3dm

                    bytes = b3dm.GlbData;
                }

                yield return ParseFromBytes(bytes, url, callbackGltf, rtcCenter);
            }

            webRequest.Dispose();
        }

        private static double[] GetRTCCenter(double[] rtcCenter, B3dm b3dm)
        {
            if (b3dm.FeatureTableJson.Length > 0)
            {
                JSONNode rootnode = JSON.Parse(b3dm.FeatureTableJson);
                var rtcCenterValues = rootnode["RTC_CENTER"];
                rtcCenter = new double[3];
                if (rtcCenterValues != null)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        rtcCenter[i] = rtcCenterValues[i].AsDouble;
                    }
                }
            }

            return rtcCenter;
        }

        /// <summary>
        /// Import binary .glb,.gltf data or get it from a .b3dm
        /// </summary>
        /// <param name="filepath">Path to local .glb,.gltf or .b3dm file</param>
        /// <param name="writeGlbNextToB3dm">Extract/copy .glb file from .b3dm and place it next to it.</param>
        public async void ImportBinFromFile(string filepath, bool writeGlbNextToB3dm = false)
        {
            byte[] bytes = null;

#if UNITY_WEBGL && !UNITY_EDITOR
            filepath = Application.persistentDataPath + "/" + filepath;
#endif

            if (Path.GetExtension(filepath).Equals(".b3dm"))
            {
                //Retrieve the glb from the b3dm
                var b3dmFileStream = File.OpenRead(filepath);
                var b3dm = B3dmReader.ReadB3dm(b3dmFileStream);
                bytes = b3dm.GlbData;

#if UNITY_EDITOR
                if (writeGlbNextToB3dm)
                {
                    var localGlbPath = filepath.Replace(".b3dm", ".glb");
                    Debug.Log("Writing local file: " + localGlbPath);
                    File.WriteAllBytes(localGlbPath, bytes);
                }
#endif
            }
            else
            {
                bytes = File.ReadAllBytes(filepath);
            }

            await ParseFromBytes(bytes, filepath, null);
        }

        /// <summary>
        /// Parse glb (or gltf) buffer bytes and do a callback when done containing a GltfImport.
        /// </summary>
        /// <param name="glbBuffer">The bytes of a glb or gtlf file</param>
        /// <param name="sourcePath">Sourcepath is required to be able to load files with external dependencies like textures etc.</param>
        /// <param name="callbackGltf">The callback containing the GltfImport result</param>
        /// <returns></returns>
        private static async Task ParseFromBytes(byte[] glbBuffer, string sourcePath, Action<ParsedGltf> callbackGltf, double[] rtcCenter = null)
        {
            //Use our parser (in this case GLTFFast to read the binary data and instantiate the Unity objects in the scene)
            var gltf = new GltfImport();
            var success = await gltf.Load(glbBuffer, new Uri(sourcePath), importSettings);

            if (success)
            {
                var parsedGltf = new ParsedGltf()
                {
                    gltfImport = gltf,
                    glbBuffer = glbBuffer,
                    rtcCenter = rtcCenter
                };
                callbackGltf?.Invoke(parsedGltf);
            }
            else
            {
                Debug.LogError($"Loading glTF failed! -> {sourcePath}");
                callbackGltf?.Invoke(null);
                gltf.Dispose();
            }
        }
    }
}

[Serializable]
public class ParsedGltf
{
    public GltfImport gltfImport;
    public byte[] glbBuffer;
    public double[] rtcCenter = null;

    private NativeArray<byte> glbBufferNative;
    private NativeArray<byte> destination;
    private NativeSlice<byte> source;

    public  Dictionary<int,Color> uniqueColors = new Dictionary<int, Color>();
    public List<int> featureTableFloats = new List<int>();

    /// <summary>
    /// Iterate through all scenes and instantiate them
    /// </summary>
    /// <param name="parent">Parent spawned scenes to Transform</param>
    /// <returns>Async Task</returns>
    public async Task SpawnGltfScenes(Transform parent)
    {
        if (gltfImport != null)
        {
            var scenes = gltfImport.SceneCount;

            for (int i = 0; i < scenes; i++)
            {
                await gltfImport.InstantiateSceneAsync(parent, i);
            }
        }
    }

    public void ParseSubObjects()
    {
        Debug.Log("Parse subobjects");
        //Extract json from glb
        var gltfAndBin = ExtractJsonAndBinary(glbBuffer);
        var gltfJsonText = gltfAndBin.Item1;
        var binaryBlob = gltfAndBin.Item2;

        Debug.Log($"Json: <color=green>{gltfJsonText}</color>");
        Debug.Log($"Bin length: <color=red>{binaryBlob.Length}</color>");

        
        //Deserialize json using JSON.net instead of Unity's JsonUtility ( gave silent error )
        var gltfFeatures = JsonConvert.DeserializeObject<GltfMeshFeatures.GltfRootObject>(gltfJsonText);

        var featureIdBufferViewIndex = 0;
        foreach(var mesh in gltfFeatures.meshes)
        {
            foreach(var primitive in mesh.primitives)
            {
                Debug.Log("_FEATURE_ID_0 : " + primitive.attributes._FEATURE_ID_0);
                featureIdBufferViewIndex = primitive.attributes._FEATURE_ID_0;
            }
        }

        Debug.Log("featureIdBufferViewIndex: " + featureIdBufferViewIndex);

        //Use feature ID as bufferView index.
        //Parse the bufferView as a feature table

        //Get bufferview
        var featureAccessor =  gltfFeatures.accessors[featureIdBufferViewIndex];
        var targetBufferView = gltfFeatures.bufferViews[featureAccessor.bufferView];

        //TODO:Check if bufferView is compressed before we try to decompress
        var featureIdBuffer = GetDecompressedBuffer(gltfFeatures.buffers, targetBufferView, binaryBlob);

        //Parse feature table into List<float>
        var stride = targetBufferView.byteStride;
        
        for (int i = 0; i < featureIdBuffer.Length; i += stride)
        {
            var featureTableIndex = (int)BitConverter.ToSingle(featureIdBuffer, i);
            featureTableFloats.Add(featureTableIndex);

            if(!uniqueColors.ContainsKey((int)featureTableIndex))
            {
                uniqueColors.Add((int)featureTableIndex, UnityEngine.Random.ColorHSV());
                if(i == 0) Debug.Log(featureTableIndex);
            }
        }

        //TODO; see how to retrieve BAGID string from bufferview
        Debug.Log("min value" + featureTableFloats.Min());
        Debug.Log("max value" + featureTableFloats.Max());

        Debug.Log("featureTableFloats count: " + featureTableFloats.Count);
        Debug.Log("uniqueColors (buildings) count: " + uniqueColors.Count);
    }

    private byte[] GetDecompressedBuffer(GltfMeshFeatures.Buffer[] buffers, GltfMeshFeatures.BufferView bufferView, byte[] glbBuffer)
    {
        //Because the mesh is compressed, we need to get the buffer and decompress it
        var bufferIndex = bufferView.extensions.EXT_meshopt_compression.buffer; //Ignore multiple buffers for now
        var byteLength = bufferView.extensions.EXT_meshopt_compression.byteLength;
        var byteOffset = bufferView.extensions.EXT_meshopt_compression.byteOffset;
        var byteStride = bufferView.extensions.EXT_meshopt_compression.byteStride;
        var count = bufferView.extensions.EXT_meshopt_compression.count;
        Debug.Log("ByteLength: " + byteLength);	
        Debug.Log("ByteOffset: " + byteOffset);
        Debug.Log("ByteStride: " + byteStride);
        Debug.Log("Count: " + count);

        //Create NativeArray from byte[] glbBuffer
        glbBufferNative = new NativeArray<byte>(glbBuffer, Allocator.Persistent);
        Debug.Log("Native buffer length: " + glbBufferNative.Length);

        //Create NativeSlice as part of the glbBuffer that the view is covering
        source = glbBufferNative.Slice((int)byteOffset, (int)byteLength);
        Debug.Log("Slice length for view region: " + source.Length);

        //Create NativeArray to store the decompressed buffer
        destination = new NativeArray<byte>(count * byteStride, Allocator.Persistent);
        Debug.Log("Destination decompressed length: " + destination.Length);

        //Decompress using meshop decomression
        Debug.Log("Decompressing");
        var success = Decode.DecodeGltfBufferSync(
            destination,
            count,
            byteStride, 
            source,            
            Meshoptimizer.Mode.Attributes,
            Meshoptimizer.Filter.None
        );
        Debug.Log(success);

        Debug.Log("Decompressed");
        return destination.ToArray();

        
    }

    public static (string, byte[]) ExtractJsonAndBinary(byte[] glbData)
    {
        if (glbData.Length < 12)
            Debug.Log("GLB file is too short.");

        // Check the magic bytes to ensure it's a GLB file
        var magicBytes = BitConverter.ToUInt32(glbData, 0);
        if (magicBytes != 0x46546C67) // "glTF"
            Debug.Log("Not a valid GLB file.");

        var version = BitConverter.ToUInt32(glbData, 4);
        var length = BitConverter.ToUInt32(glbData, 8);

        if (version != 2)
            Debug.Log("Unsupported GLB version.");

        if (glbData.Length != length)
            Debug.Log("GLB file length does not match the declared length.");

        // Find the JSON chunk
        var jsonChunkLength = BitConverter.ToUInt32(glbData, 12);
        if (jsonChunkLength == 0)
            Debug.Log("JSON chunk is missing.");
        var jsonChunkOffset = 20; // GLB header (12 bytes) + JSON chunk header (8 bytes)

        // Extract JSON as a string
        var json = Encoding.UTF8.GetString(glbData, jsonChunkOffset, (int)jsonChunkLength);

        // Find the binary chunk
        var binaryChunkLength = length - jsonChunkLength - 28; // 28 = GLB header (12 bytes) + JSON chunk header (8 bytes) + JSON chunk length (4 bytes) + BIN chunk header (8 bytes)
        if (binaryChunkLength == 0)
            Debug.Log("BIN chunk is missing.");
        var binaryChunkOffset = jsonChunkOffset + (int)jsonChunkLength + 8; // JSON chunk header (8 bytes) + JSON chunk length (4 bytes)

        // Extract binary data as a byte array
        var binaryData = new byte[binaryChunkLength];
        Buffer.BlockCopy(glbData, binaryChunkOffset, binaryData, 0, (int)binaryChunkLength);

        return (json, binaryData);
    }
}


