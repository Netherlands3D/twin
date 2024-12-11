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
using System.Numerics;
using static Netherlands3D.Tiles3D.FeatureTable;
namespace Netherlands3D.Tiles3D
{
    public class ImportGltf
    {
        private static ImportSettings importSettings = new ImportSettings() { AnimationMethod = AnimationMethod.None };

        public async Task Load(byte[] data, Tile tile, Transform containerTransform, Action<bool> succesCallback, string sourcePath, bool parseAssetMetaData = false, bool parseSubObjects = false, UnityEngine.Material overrideMaterial = null)
        {
            var binaryData = data;
            var gltf = new GltfImport();
            var success = true;
            Uri uri = null;
            if (sourcePath != "")
            {
                uri = new Uri(sourcePath);
            }
            

            //remove the ?-part from material-uri's to test with local dataset
            //string jsonstring = Encoding.UTF8.GetString(binaryData);
            //JSONNode root = JSON.Parse(jsonstring);
            //JSONNode imagesNode = root["images"];
            //if (imagesNode != null)
            //{
            //    for (int i = 0; i < imagesNode.Count; i++)
            //    {
            //        JSONNode urinode = imagesNode[i]["uri"];
            //        if (urinode!=null)
            //        {
            //            urinode.Value = urinode.Value.Split("?")[0];
            //        }
            //    }
            //}

           //binaryData = Encoding.UTF8.GetBytes(root.ToString());

            //RemoveCesiumRtcFromRequieredExtentions(ref data);

            Debug.Log("starting gltfLoad");
            success = await gltf.Load(uri);
            //success = await gltf.LoadGltfBinary(data, uri, importSettings);

            Debug.Log("gltfLoad has finished");

            if (success == false)
            {
                Debug.Log("cant load gltf: " + sourcePath);
                succesCallback.Invoke(false);
                return;
            }
            //double[] rtcCenter = GetRTCCenterFromGlb(data);
            var parsedGltf = new ParsedGltf()
            {
                gltfImport = gltf,
                //rtcCenter = rtcCenter,
            };
            await parsedGltf.SpawnGltfScenes(containerTransform);

            containerTransform.gameObject.name = sourcePath;

            if (parseAssetMetaData)
            {
                Content content = containerTransform.GetComponent<Content>();
                if (content != null)
                {
                    // parsedGltf.ParseAssetMetaData(content);
                }

            }

            //Check if mesh features addon is used to define subobjects
#if SUBOBJECT
            if (parseSubObjects)
            {
                // parsedGltf.ParseSubObjects(containerTransform);
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
            if (rtcCenter != null)
            {
                sceneCoordinate = new Coordinate(CoordinateSystem.WGS84_ECEF, rtcCenter[0], rtcCenter[1], rtcCenter[2]);
            }
            Coordinate transformedCoordinate = tileTransform.MultiplyPoint3x4(sceneCoordinate);
            scene.position = transformedCoordinate.ToUnity();
            scene.rotation = transformedCoordinate.RotationToLocalGravityUp();

        }
        private static double[] GetRTCCenterFromGlb(byte[] GlbData)
        {

            int jsonstart = 20;
            int jsonlength = (GlbData[15]) * 256;
            jsonlength = (jsonlength + GlbData[14]) * 256;
            jsonlength = (jsonlength + GlbData[13]) * 256;
            jsonlength = (jsonlength + GlbData[12]);

            string gltfjsonstring = Encoding.UTF8.GetString(GlbData, jsonstart, jsonlength);


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

        private static void RemoveCesiumRtcFromRequieredExtentions(ref byte[] GlbData)
        {
            int jsonstart = 20;
            int jsonlength = (GlbData[15]) * 256;
            jsonlength = (jsonlength + GlbData[14]) * 256;
            jsonlength = (jsonlength + GlbData[13]) * 256;
            jsonlength = (jsonlength + GlbData[12]);

            string jsonstring = Encoding.UTF8.GetString(GlbData, jsonstart, jsonlength);

            JSONNode gltfJSON = JSON.Parse(jsonstring);
            JSONNode extensionsRequiredNode = gltfJSON["extensionsRequired"];
            if (extensionsRequiredNode == null)
            {
                return;
            }
            int extensionsRequiredCount = extensionsRequiredNode.Count;
            int cesiumRTCIndex = -1;
            for (int ii = 0; ii < extensionsRequiredCount; ii++)
            {
                if (extensionsRequiredNode[ii].Value == "CESIUM_RTC")
                {
                    cesiumRTCIndex = ii;
                }
            }
            if (cesiumRTCIndex < 0)
            {
                return;
            }


            if (extensionsRequiredCount == 1)
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
            for (i = 0; i < resultbytes.Length; i++)
            {
                GlbData[jsonstart + i] = resultbytes[i];
            }
            for (int j = i; j < jsonlength; j++)
            {
                GlbData[jsonstart + j] = 0x20;
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
