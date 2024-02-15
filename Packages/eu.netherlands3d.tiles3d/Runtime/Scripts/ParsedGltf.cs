using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using GLTFast;
using System;
using System.Text.RegularExpressions;
using System.Text;
using Newtonsoft.Json;
using Meshoptimizer;
using Unity.Collections;

#if UNITY_EDITOR
using System.Linq;
#endif

#if SUBOBJECT
using Netherlands3D.SubObjects;
#endif

namespace Netherlands3D.Tiles3D
{
    [Serializable]
    public class ParsedGltf
    {
        public GltfImport gltfImport;
        public byte[] glbBuffer;
        public double[] rtcCenter = null;

        private NativeArray<byte> glbBufferNative;
        private NativeArray<byte> destination;
        private NativeSlice<byte> source;

        public Dictionary<int, Color> uniqueColors = new Dictionary<int, Color>();
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

                //Spawn all scenes (InstantiateMainSceneAsync only possible if main scene was referenced in gltf)
                for (int i = 0; i < scenes; i++)
                {
                    await gltfImport.InstantiateSceneAsync(parent, i);
                }
            }
        }

        /// <summary>
        /// Parse subobjects from gltf data
        /// </summary>
        /// <param name="parent">Parent transform where scenes were spawned in</param>
        public void ParseSubObjects(Transform parent)
        {
            //Extract json from glb
            var gltfAndBin = ExtractJsonAndBinary(glbBuffer);
            var gltfJsonText = gltfAndBin.Item1;
            var binaryBlob = gltfAndBin.Item2;

            //Deserialize json using JSON.net instead of Unity's JsonUtility ( gave silent error )
            var gltfFeatures = JsonConvert.DeserializeObject<GltfMeshFeatures.GltfRootObject>(gltfJsonText);

            var featureIdBufferViewIndex = 0;
            foreach (var mesh in gltfFeatures.meshes)
            {
                foreach (var primitive in mesh.primitives)
                {
                    featureIdBufferViewIndex = primitive.attributes._FEATURE_ID_0;
                }
            }
            if (featureIdBufferViewIndex == -1)
            {
                Debug.LogWarning("_FEATURE_ID_0 was not found in the dataset. This is required to find BAG id's.");
                return;
            }

            //Use feature ID as bufferView index and get bufferview
            var featureAccessor = gltfFeatures.accessors[featureIdBufferViewIndex];
            var targetBufferView = gltfFeatures.bufferViews[featureAccessor.bufferView];

            // var compressed = gltfFeatures.extensionsRequired.Contains("EXT_meshopt_compression"); //Needs testing
            var compressed = false;

            var featureIdBuffer = GetFeatureBuffer(gltfFeatures.buffers, targetBufferView, binaryBlob, compressed);
            if (featureIdBuffer == null || featureIdBuffer.Length == 0)
            {
                Debug.LogWarning("Getting feature buffer failed.");
                return;
            }

            //Parse feature table into List<float>
            List<Vector2Int> vertexFeatureIds = new();
            var stride = targetBufferView.byteStride;
            int currentFeatureTableIndex = -1;
            int vertexCount = 0;
            int accessorOffset = featureAccessor.byteOffset;
            for (int i = 0; i < featureIdBuffer.Length; i += stride)
            {
                //TODO: Read componentType from accessor to determine how to read the featureTableIndex
                var featureTableIndex = (int)BitConverter.ToSingle(featureIdBuffer, i+accessorOffset); 
                
                if (currentFeatureTableIndex != featureTableIndex)
                {
                    if (currentFeatureTableIndex != -1)
                        vertexFeatureIds.Add(new Vector2Int(currentFeatureTableIndex, vertexCount));

                    currentFeatureTableIndex = featureTableIndex;
                    vertexCount = 1;
                }
                else
                {
                    vertexCount++;
                }
            }
            //Finish last feature table entry
            vertexFeatureIds.Add(new Vector2Int(currentFeatureTableIndex, vertexCount));

            //Retrieve EXT_structural_metadata tables
            var propertyTables = gltfFeatures.extensions.EXT_structural_metadata.propertyTables;

            //Now parse the property tables BAGID 
            var bagIdList = new List<string>();
            foreach (var propertyTable in propertyTables)
            {
                //Now parse the data from the buffer using stringOffsetType=UINT32
                var bagpandid = propertyTable.properties.bagpandid; //Based on Tyler dataset key naming
                var identificatie = propertyTable.properties.identificatie;  //Based on PG2B3DM dataset key naming

                var bufferViewIndex = (bagpandid != null) ? bagpandid.values : identificatie.values; //Values reference the bufferView index
                var count = propertyTable.count;
                var bufferView = gltfFeatures.bufferViews[bufferViewIndex];
                var stringSpan = bufferView.byteLength / count; //string length in bytes

                //Directly convert the buffer to a list of strings
                var stringBytesSpan = new Span<byte>(binaryBlob, (int)bufferView.byteOffset, bufferView.byteLength);
                for (int i = 0; i < count; i++)
                {
                    var stringBytesSpanSlice = stringBytesSpan.Slice(i * stringSpan, stringSpan);
                    var stringBytes = stringBytesSpanSlice.ToArray();
                    var stringBytesString = Encoding.ASCII.GetString(stringBytes);
                    bagIdList.Add(stringBytesString);
                }
                break; //Just support one for now.
            }

#if SUBOBJECT

            foreach (Transform child in parent)
            {
                Debug.Log(child.name,child.gameObject);
                //Add subobjects to the spawned gameobject
                child.gameObject.AddComponent<MeshCollider>();
                ObjectMapping objectMapping = child.gameObject.AddComponent<ObjectMapping>();
                objectMapping.items = new List<ObjectMappingItem>();

                //For each uniqueFeatureIds, add a subobject
                int offset = 0;
                for (int i = 0; i < vertexFeatureIds.Count; i++)
                {
                    var uniqueFeatureId = vertexFeatureIds[i];
                    var bagId = bagIdList[uniqueFeatureId.x];
                    
                    //Remove any prefixes/additions to the bag id
                    bagId = Regex.Replace(bagId, "[^0-9]", "");

                    var subObject = new ObjectMappingItem()
                    {
                        objectID = bagId,
                        firstVertex = offset,
                        verticesLength = uniqueFeatureId.y
                    };
                    objectMapping.items.Add(subObject);
                    offset += uniqueFeatureId.y;
                }
            }
            return;
#endif
            Debug.LogWarning("Subobjects are not supported in this build. Please use the Netherlands3D.SubObjects package.");
        }

        private byte[] GetFeatureBuffer(GltfMeshFeatures.Buffer[] buffers, GltfMeshFeatures.BufferView bufferView, byte[] glbBuffer, bool decompress)
        {
            //Because the mesh is compressed, we need to get the buffer and decompress it
            var byteLength = decompress ? bufferView.extensions.EXT_meshopt_compression.byteLength : bufferView.byteLength;
            var byteOffset = decompress ? bufferView.extensions.EXT_meshopt_compression.byteOffset : bufferView.byteOffset;

            //Create NativeArray from byte[] glbBuffer
            glbBufferNative = new NativeArray<byte>(glbBuffer, Allocator.Persistent);

            //Create NativeSlice as part of the glbBuffer that the view is covering
            source = glbBufferNative.Slice((int)byteOffset, (int)byteLength);
            if(!decompress)
            {
                //Convert slice to byte[] array
                Debug.Log("Use buffer directly");
                return source.ToArray();
            }
            Debug.Log("Decompress buffer");
            var byteStride = bufferView.extensions.EXT_meshopt_compression.byteStride;
            var count = bufferView.extensions.EXT_meshopt_compression.count;   

            //Create NativeArray to store the decompressed buffer
            destination = new NativeArray<byte>(count * byteStride, Allocator.Persistent);

            //Decompress using meshop decomression
            var success = Decode.DecodeGltfBufferSync(
                destination,
                count,
                byteStride,
                source,
                Meshoptimizer.Mode.Attributes,
                Meshoptimizer.Filter.None
            );

            return destination.ToArray();
        }

        public static (string, byte[]) ExtractJsonAndBinary(byte[] glbData)
        {
            if (glbData.Length < 12)
                Debug.Log("GLB file is too short.");

            //Check the magic bytes to ensure it's a GLB file
            var magicBytes = BitConverter.ToUInt32(glbData, 0);
            if (magicBytes != 0x46546C67) // "glTF"
                Debug.Log("Not a valid GLB file.");

            var version = BitConverter.ToUInt32(glbData, 4);
            var length = BitConverter.ToUInt32(glbData, 8);

            if (version != 2)
                Debug.Log("Unsupported GLB version.");

            if (glbData.Length != length)
                Debug.Log("GLB file length does not match the declared length.");

            //Find the JSON chunk
            var jsonChunkLength = BitConverter.ToUInt32(glbData, 12);
            if (jsonChunkLength == 0)
                Debug.Log("JSON chunk is missing.");
            var jsonChunkOffset = 20; //GLB header (12 bytes) + JSON chunk header (8 bytes)

            //Extract JSON as a string
            var json = Encoding.UTF8.GetString(glbData, jsonChunkOffset, (int)jsonChunkLength);

            // Find the binary chunk
            var binaryChunkLength = length - jsonChunkLength - 28; //28 = GLB header (12 bytes) + JSON chunk header (8 bytes) + JSON chunk length (4 bytes) + BIN chunk header (8 bytes)
            if (binaryChunkLength == 0)
                Debug.Log("BIN chunk is missing.");
            var binaryChunkOffset = jsonChunkOffset + (int)jsonChunkLength + 8; //JSON chunk header (8 bytes) + JSON chunk length (4 bytes)

            //Extract binary data as a byte array
            var binaryData = new byte[binaryChunkLength];
            Buffer.BlockCopy(glbData, binaryChunkOffset, binaryData, 0, (int)binaryChunkLength);

            return (json, binaryData);
        }
    }
}