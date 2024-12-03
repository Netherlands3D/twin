using GLTFast;
using Netherlands3D.Coordinates;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
//using UnityEditor.AssetImporters;
using UnityEngine;
using static Netherlands3D.Tiles3D.FeatureTable;

namespace Netherlands3D.Tiles3D
{
//    public class ImportI3dm: ScriptedImporter
//    {
//        private static ImportSettings importSettings = new ImportSettings() { AnimationMethod = AnimationMethod.None };
//        public static async Task Load(byte[] data, Tile tile, Transform containerTransform, Action<bool> succesCallback, string sourcePath, bool parseAssetMetaData = false, bool parseSubObjects = false, UnityEngine.Material overrideMaterial = null)
//        {

//            Debug.Log("loading i3dm");
//            var memoryStream = new System.IO.MemoryStream(data);

//            var header = new I3dmHeader(memoryStream);
//            int headerLength = 32;

//            // get the geometry
//            var gltf = new GltfImport();
//            var success = await gltf.Load(header.glbBuffer, new Uri(sourcePath), importSettings);
//            if (success == false)
//            {
//                Debug.LogError("could not load Gltf");
//                succesCallback.Invoke(false);
//                return;
//            }

//            string featureTableJSONString = Encoding.UTF8.GetString(data, headerLength, header.FeatureTableJsonByteLength);
//            int featureDataStart = headerLength + header.FeatureTableJsonByteLength;

//            JSONNode featureTable = JSON.Parse(featureTableJSONString);

//            //required

//            JSONNode node;
//            JSONNode offsetNode;

//            node = featureTable["INSTANCES_LENGTH"];
//            if (node == null)
//            {
//                Debug.LogError("could not find Instances_Length");
//                succesCallback.Invoke(false);
//                return;
//            }
//            int pointsLength = node.AsInt;

//            Coordinate RTC_Center;
//            bool usesRtcCenter = FeatureTable.RTC_CENTER.TryRead(featureTable, out RTC_Center);
//            RTC_Center = tile.tileTransform.MultiplyPoint3x4(RTC_Center);

//            Vector3[] verts = FeatureTable.Positions.Read(featureTable, memoryStream, featureDataStart, pointsLength);

//            var parsedGltf = new ParsedGltf()
//            {
//                gltfImport = gltf,
//                rtcCenter = RTC_Center.Points,
//#if SUBOBJECT
//                glbBuffer = header.glbBuffer //Store the glb buffer for access in subobjects
//#endif
//            };

//            Vector3 RTCCenter = RTC_Center.ToUnity();
//            await parsedGltf.SpawnGltfScenes(containerTransform);
//            Transform original = containerTransform.GetChild(0);
//            original.position = RTCCenter + verts[0];
//            //original.rotation = RTC_Center.RotationToLocalGravityUp();
//            GameObject instance = original.gameObject;
            
//            for (int i = 1; i < verts.Length; i++)
//            {
//                Vector3 position = RTCCenter + verts[i];
//                GameObject clone = Instantiate(instance,containerTransform);
//                clone.transform.position = RTCCenter + verts[i];
//            }
//            succesCallback.Invoke(true);
//        }

//        public override void OnImportAsset(AssetImportContext ctx)
//        {
//            throw new NotImplementedException();
//        }

//        private struct I3dmHeader
//        {
//            public string Magic { get; set; }
//            public int Version { get; set; }
//            public int fileLength { get; set; }
//            public int FeatureTableJsonByteLength { get; set; }
//            public int FeatureTableBinaryByteLength { get; set; }
//            public int BatchTableJsonByteLength { get; set; }
//            public int BatchTableBinaryByteLength { get; set; }
//            public int GltfFormat { get; set; }
//            public byte[] glbBuffer { get; }

//            public I3dmHeader(MemoryStream memorystream)
//            {
//                var reader = new BinaryReader (memorystream);

//                Magic = Encoding.UTF8.GetString(reader.ReadBytes(4));
//                Version = (int)reader.ReadUInt32();
//                fileLength = (int)reader.ReadUInt32();

//                FeatureTableJsonByteLength = (int)reader.ReadUInt32();
//                FeatureTableBinaryByteLength = (int)reader.ReadUInt32();
//                BatchTableJsonByteLength = (int)reader.ReadUInt32();
//                BatchTableBinaryByteLength = (int)reader.ReadUInt32();
//                GltfFormat = (int)reader.ReadUInt32();


//                var glbLength = fileLength - 32;

//                glbBuffer = reader.ReadBytes(glbLength);

//                // remove the trailing glb padding characters if any

//                //int stride = 8;
//                List<byte> bytes = new List<byte>();
//                bytes.Capacity = glbBuffer.Length;
//                for (int i = 0; i < glbLength; i++)
//                {

//                    bytes.Add(glbBuffer[i]);

//                }
//                //readGltfByteSize
//                glbLength = bytes[11] * 256;
//                glbLength = (glbLength + bytes[10]) * 256;
//                glbLength = (glbLength + bytes[9]) * 256;
//                glbLength = glbLength + bytes[8];

//                for (int i = bytes.Capacity - 1; i >= glbLength; i--)
//                {
//                    bytes.RemoveAt(i);
//                }

//                glbBuffer = bytes.ToArray();

//            }
//        }
        
//    }
}
