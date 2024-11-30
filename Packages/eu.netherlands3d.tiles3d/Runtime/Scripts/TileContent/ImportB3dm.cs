using GLTFast;
using Netherlands3D.Coordinates;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace Netherlands3D.Tiles3D
{
    public class ImportB3dm
    {

        private static ImportSettings importSettings = new ImportSettings() { AnimationMethod = AnimationMethod.None };

        public static async Task LoadB3dm(byte[] data,Tile tile,Transform containerTransform,  Action<bool> succesCallback, string sourcePath,bool parseAssetMetaData=false,bool parseSubObjects=false,UnityEngine.Material overrideMaterial=null)
        { 
            

            var memoryStream = new System.IO.MemoryStream(data);
            var b3dm = B3dmReader.ReadB3dm(memoryStream);


            double[] rtcCenter = GetRTCCenterFromB3dm(b3dm);

            RemoveCesiumRtcFromRequieredExtentions(ref b3dm);
            if (rtcCenter==null)
            {

               
                rtcCenter = GetRTCCenterFromGlb(b3dm);


            }
            

            var gltf = new GltfImport();
            var success = await gltf.Load(b3dm.GlbData, new Uri(sourcePath), importSettings);
            if (success == false)
            {
                Debug.Log("cant load glb: " + sourcePath);
                succesCallback.Invoke(false);
                return;
            }
            var parsedGltf = new ParsedGltf()
            {
                gltfImport = gltf,
                rtcCenter = rtcCenter,
#if SUBOBJECT
                glbBuffer = b3dm.GlbData //Store the glb buffer for access in subobjects
#endif
            };
            await parsedGltf.SpawnGltfScenes(containerTransform);

            containerTransform.gameObject.name = sourcePath;

            if (parseAssetMetaData)
            {
                Content content = containerTransform.GetComponent<Content>();
                if (content!=null)
                {
                    parsedGltf.ParseAssetMetaData(content);
                }
                
            }

            //Check if mesh features addon is used to define subobjects
#if SUBOBJECT
            if (parseSubObjects)
            {
                parsedGltf.ParseSubObjects(containerTransform);
            }
#endif

            if (overrideMaterial != null)
            {
                parsedGltf.OverrideAllMaterials(overrideMaterial);
            }



            succesCallback.Invoke(true);
        }
        static void PositionGameObject(Transform scene, double[] rtcCenter, TileTransform tileTransform)
        {
            Coordinate sceneCoordinate = new Coordinate(CoordinateSystem.WGS84_ECEF, -scene.localPosition.x, -scene.localPosition.z, scene.localPosition.y);
            if (rtcCenter!=null)
            {
                sceneCoordinate = new Coordinate(CoordinateSystem.WGS84_ECEF, rtcCenter[0], rtcCenter[1], rtcCenter[2]);
            }
            Coordinate transformedCoordinate = tileTransform.MultiplyPoint3x4(sceneCoordinate);
            scene.position = transformedCoordinate.ToUnity();
            scene.rotation = transformedCoordinate.RotationToLocalGravityUp();

        }
        private static double[] GetRTCCenterFromB3dm(B3dm b3dm)
        {
            string batchttableJSONstring = b3dm.FeatureTableJson;
            JSONNode root = JSON.Parse(batchttableJSONstring);
            JSONNode centernode = root["RTC_CENTER"];
            if (centernode == null)
            {
                return null;
            }
            if (centernode.Count != 3)
            {
                return null;
            }
            double[] result = new double[3];
            result[0] = centernode[0].AsDouble;
            result[1] = centernode[1].AsDouble;
            result[2] = centernode[2].AsDouble;
            return result;

        }
        private static double[] GetRTCCenterFromGlb(B3dm b3dm)
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
                if (extensionsNode == null)
                {
                    return null;
                }
                JSONNode cesiumRTCNode = extensionsNode["CESIUM_RTC"];
                if (cesiumRTCNode == null)
                {
                    return null;
                }
                JSONNode centernode = cesiumRTCNode["center"];
                if (centernode == null)
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
        private static void RemoveCesiumRtcFromRequieredExtentions(ref B3dm b3dm)
        {
            int jsonstart = 20;
            int jsonlength = (b3dm.GlbData[15]) * 256;
            jsonlength = (jsonlength + b3dm.GlbData[14]) * 256;
            jsonlength = (jsonlength + b3dm.GlbData[13]) * 256;
            jsonlength = (jsonlength + b3dm.GlbData[12]);

            string jsonstring = Encoding.UTF8.GetString(b3dm.GlbData, jsonstart, jsonlength);

            JSONNode gltfJSON = JSON.Parse(jsonstring);
            JSONNode extensionsRequiredNode = gltfJSON["extensionsRequired"];
            if (extensionsRequiredNode==null)
            {
                return;
            }
            int extensionsRequiredCount = extensionsRequiredNode.Count;
            int cesiumRTCIndex = -1;
            for (int ii = 0; ii < extensionsRequiredCount; ii++)
            {
                if (extensionsRequiredNode[ii].Value== "CESIUM_RTC")
                {
                    cesiumRTCIndex = ii;
                }
            }
            if (cesiumRTCIndex<0)
            {
                return;
            }
           
            
            if (extensionsRequiredCount==1)
            {
                gltfJSON.Remove(extensionsRequiredNode);
            }
            else
            {
                extensionsRequiredNode.Remove(cesiumRTCIndex);
            }
            jsonstring = gltfJSON.ToString();
           
            byte[] resultbytes = Encoding.UTF8.GetBytes(jsonstring);

            int i = 0;
            for ( i = 0; i < resultbytes.Length; i++)
            {
                b3dm.GlbData[jsonstart+i] = resultbytes[i];
            }
            for (int j = i; j < jsonlength; j++)
            {
                b3dm.GlbData[jsonstart+j] = 0x20;
            }
            
            return;
            //string ExtentionsRequiredString = "\"extensionsRequired\"";
            //int extentionsStart = jsonstring.IndexOf(ExtentionsRequiredString);
            //if (extentionsStart < 0)
            //{
            //    return;
            //}
            //int extentionstringEnd = extentionsStart + ExtentionsRequiredString.Length;

            //int arrayEnd = jsonstring.IndexOf("]", extentionstringEnd);
            //string cesiumString = "\"CESIUM_RTC\"";
            //int cesiumstringStart = jsonstring.IndexOf(cesiumString, extentionstringEnd);
            //if (cesiumstringStart < 0)
            //{
            //    Debug.Log("no cesium_rtc required");
            //    return;
            //}
            //Debug.Log("cesium_rtc required");
            //int cesiumstringEnd = cesiumstringStart + cesiumString.Length;
            //int seperatorPosition = jsonstring.IndexOf(",", extentionstringEnd);


            //int removalStart = cesiumstringStart;
            //int removalEnd = cesiumstringEnd;
            //if (seperatorPosition > arrayEnd)
            //{
            //    removalStart = extentionsStart - 1;
            //    removalEnd = arrayEnd + 1;
            //}
            //else
            //{
            //    if (seperatorPosition < cesiumstringStart)
            //    {
            //        removalStart = seperatorPosition;
            //    }
            //    if (seperatorPosition > cesiumstringEnd)
            //    {
            //        removalEnd = seperatorPosition;
            //    }
            //}

            //for (int i = removalStart; i < removalEnd; i++)
            //{
            //    b3dm.GlbData[i + jsonstart] = 0x20;
            //}








        }
    }
}
