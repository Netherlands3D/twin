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
#if UNITY_EDITOR
using System.IO.Compression;
#endif
namespace Netherlands3D.Tiles3D
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
        /// <param name="bypassCertificateValidation"></param>
        /// <returns></returns>
        public static IEnumerator ImportBinFromURL(string url, Action<ParsedGltf> callbackGltf, bool bypassCertificateValidation = false, Dictionary<string,string> customHeaders = null)
        {
            var webRequest = UnityWebRequest.Get(url);
            if(customHeaders != null){
                foreach (var header in customHeaders)
                    webRequest.SetRequestHeader(header.Key, header.Value);
            }
            
            if (bypassCertificateValidation)
                webRequest.certificateHandler = customCertificateHandler; //Not safe; but solves breaking curl error

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning(url + " -> " +webRequest.error);
                callbackGltf.Invoke(null);
            }
            else
            {
                byte[] bytes = webRequest.downloadHandler.data;
                double[] rtcCenter = null;

                //readMagic
                string magic = Encoding.UTF8.GetString(bytes,0, 4);


                if (magic == "b3dm")
                {
                    var memoryStream = new MemoryStream(bytes);

                    var b3dm = B3dmReader.ReadB3dm(memoryStream);
                    rtcCenter = GetRTCCenterFromB3dm(b3dm);
                    if (rtcCenter == null)
                    { 
                        rtcCenter = GetRTCCenterFromGlb(b3dm);
                        if (rtcCenter != null)
                        {
                            RemoveCesiumRtcFromRequieredExtentions(ref b3dm);
                        }
                    }

                bytes = b3dm.GlbData;
                    //write the glb-blob to disk for analysis
                    int finalSlash = url.LastIndexOf("/");
                    string filename = "c:/test/" + url.Substring(finalSlash).Replace(".b3dm", ".glb");
                    File.WriteAllBytes(filename, bytes);

                }

                //else sdfsdfsdf
                yield return ParseFromBytes(bytes, url, callbackGltf, rtcCenter);
            }

            webRequest.Dispose();
        }

        private static void RemoveCesiumRtcFromRequieredExtentions(ref B3dm b3dm)
        {
            int jsonstart = 20;
            int jsonlength = (b3dm.GlbData[15]) * 256;
            jsonlength = (jsonlength+b3dm.GlbData[14]) * 256;
            jsonlength = (jsonlength + b3dm.GlbData[13]) * 256;
            jsonlength = (jsonlength + b3dm.GlbData[12]);

            string jsonstring = Encoding.UTF8.GetString(b3dm.GlbData, jsonstart, jsonlength);

            string ExtentionsRequiredString = "\"extensionsRequired\"" ;
            int extentionsStart = jsonstring.IndexOf(ExtentionsRequiredString);
            if (extentionsStart < 0)
            {
                return;
            }
            int extentionstringEnd = extentionsStart + ExtentionsRequiredString.Length;

            int arrayEnd = jsonstring.IndexOf("]", extentionstringEnd);
            string cesiumString = "\"CESIUM_RTC\"";
            int cesiumstringStart = jsonstring.IndexOf(cesiumString, extentionstringEnd);
            if (cesiumstringStart < 0)
            {
                return;
            }
            int cesiumstringEnd = cesiumstringStart + cesiumString.Length;
            int seperatorPosition = jsonstring.IndexOf(",", extentionstringEnd);


            int removalStart=cesiumstringStart;
            int removalEnd = cesiumstringEnd;
            if (seperatorPosition > arrayEnd)
            {
                removalStart = extentionsStart-1;
                removalEnd = arrayEnd+1;
            }
            else 
            { 
                if (seperatorPosition < cesiumstringStart)
                {
                    removalStart = seperatorPosition;
                }
                if (seperatorPosition > cesiumstringEnd)
                {
                    removalEnd = seperatorPosition;
                }
            }

            for (int i = removalStart; i < removalEnd; i++)
            {
                b3dm.GlbData[i+ jsonstart] = 0x20;
            }





            
           
            
        }
       
        private static double[] GetRTCCenterFromB3dm(B3dm b3dm)
        {
            string batchttableJSONstring = b3dm.FeatureTableJson;
            JSONNode root = JSON.Parse(batchttableJSONstring);
            JSONNode centernode = root["RTC_CENTER"];
            if (centernode==null)
            {
                return null;
            }
            if (centernode.Count!=3)
            {
                return null;
            }
            double[] result = new double[3];
            result[0] = centernode[0].AsDouble;
            result[1]=centernode[1].AsDouble;
            result[2]=centernode[2].AsDouble;
            return result;

        }
        private static double[] GetRTCCenterFromGlb( B3dm b3dm)
        {

            int jsonstart = 20;
            int jsonlength = (b3dm.GlbData[15]) * 256;
            jsonlength = (jsonlength + b3dm.GlbData[14]) * 256;
            jsonlength = (jsonlength + b3dm.GlbData[13]) * 256;
            jsonlength = (jsonlength + b3dm.GlbData[12]);
            
            string gltfjsonstring = Encoding.UTF8.GetString(b3dm.GlbData, jsonstart, jsonlength);


            if (gltfjsonstring.Length > 0)
            {

                JSONNode rootnode = JSON.Parse(gltfjsonstring);
                JSONNode extensionsNode = rootnode["extensions"];
                if (extensionsNode==null)
                {
                    return null;
                }
                JSONNode cesiumRTCNode = extensionsNode["CESIUM_RTC"];
                if (cesiumRTCNode==null)
                {
                    return null;
                }
                JSONNode centernode = cesiumRTCNode["center"];
                if (centernode==null)
                {
                    return null;
                }
                
                    double[] rtcCenter = new double[3];

                        for (int i = 0; i < 3; i++)
                        {
                            rtcCenter[i] = centernode[i].AsDouble;
                        }
                        return rtcCenter;

               
               
            }

            return null;
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
                    rtcCenter = rtcCenter,
#if SUBOBJECT
                    glbBuffer = glbBuffer //Store the glb buffer for access in subobjects
#endif
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
