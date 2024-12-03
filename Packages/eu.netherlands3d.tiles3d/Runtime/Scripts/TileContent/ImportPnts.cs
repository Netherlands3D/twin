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
    public static class ImportPnts
    {
        public static async Task LoadPoints(byte[] data, Tile tile, Transform containerTransform, Action<bool> succesCallback, string sourcePath, bool parseAssetMetaData = false, bool parseSubObjects = false, UnityEngine.Material overrideMaterial = null)
        {
            var memoryStream = new System.IO.MemoryStream(data);

            var reader = new BinaryReader(memoryStream) ;
            var header = new B3dmHeader(reader);
            int headerLength = 28;

            string featureTableJSONString = Encoding.UTF8.GetString(data, headerLength, header.FeatureTableJsonByteLength);
            int featureDataStart = headerLength + header.FeatureTableJsonByteLength;

            ////write the glb-blob to disk for analysis
            //int finalSlash = sourcePath.LastIndexOf("/");
            //string filename = "c:/test/" + sourcePath.Substring(finalSlash).Replace(".b3dm", ".json");
            //File.WriteAllText(filename, featureTableJSONString);
            //return;
            JSONNode featureTable = JSON.Parse(featureTableJSONString);
            
            //required
            
            JSONNode node;
            JSONNode offsetNode;

            node = featureTable["POINTS_LENGTH"];
            if (node == null)
            {
                succesCallback.Invoke(false);
                return;
            }
            int pointsLength = node.AsInt;

            Coordinate RTC_Center ;
            bool usesRtcCenter = FeatureTable.RTC_CENTER.TryRead(featureTable, out RTC_Center);
            RTC_Center = tile.tileTransform.MultiplyPoint3x4(RTC_Center);

            Vector3[] verts = FeatureTable.Positions.Read(featureTable, memoryStream, featureDataStart, pointsLength);

            
            int[] indices = new int[pointsLength];
            for (int i = 0; i < pointsLength; i++)
            {
                indices[i] = i;
            }

            Color32[] colors = FeatureTable.Color.GetColor(featureTable, memoryStream, featureDataStart, pointsLength);
            


            Mesh mesh = new Mesh();
            mesh.vertices = verts;
            mesh.SetIndices(indices, MeshTopology.Points, 0);
            if (colors!=null)
            {
                mesh.colors32 = colors;
            }
            
            GameObject gameObject = new GameObject();
            gameObject.transform.parent = containerTransform;
            gameObject.transform.position = RTC_Center.ToUnity();
            gameObject.transform.rotation = RTC_Center.RotationToLocalGravityUp() * gameObject.transform.rotation;

            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            Shader shader = Shader.Find("Shader Graphs/Pointcloud");
            
            Material material = new Material(shader);
            meshRenderer.material = material;
            succesCallback.Invoke(true);
        }

        
        
    }

    public static class FeatureTable
    {
        public static class Color
        {
            internal static Color32[] GetColor(JSONNode json, MemoryStream memoryStream, int featureDataSTart, int pointsLength)
            {
                Color32[] result = TryGetRGBA(json, memoryStream, featureDataSTart, pointsLength);
                if (result != null) return result;

                result = TryGetRGB(json, memoryStream, featureDataSTart, pointsLength);
                if (result != null) return result;

                result = TryGetRGB565(json, memoryStream, featureDataSTart, pointsLength);
                if (result != null) return result;

                result = TryGetConstant_RGBA(json, pointsLength);
                return result;
            }
            internal static Color32[] TryGetConstant_RGBA(JSONNode json, int pointslength)
            {
                JSONNode node = json["CONSTANT_RGBA"];
                if (node == null) return null;
                if (node.Count != 4) return null;
                byte r = (byte)node[0].AsInt;
                byte g = (byte)node[1].AsInt;
                byte b = (byte)node[2].AsInt;
                byte a = (byte)node[3].AsInt;
                Color32 color = new Color32(r, g, b, a);

                Color32[] result = new Color32[pointslength];
                for (int i = 0; i < pointslength; i++)
                {
                    result[i] = color;
                }
                return result;
            }

            private static Color32[] TryGetRGB(JSONNode json, MemoryStream memoryStream, int featureDataSTart, int pointsLength)
            {
                JSONNode node = json["RGB"];
                if (node == null) return null;
                JSONNode offsetNode = node["byteOffset"];
                if (offsetNode == null) return null;
                int colorStart = offsetNode.AsInt;
                memoryStream.Position = featureDataSTart + colorStart;
                var reader = new BinaryReader(memoryStream);

                Color32[] result = new Color32[pointsLength];
                byte a = 255;
                for (int i = 0; i < pointsLength; i++)
                {
                    byte r = reader.ReadByte();
                    byte g = reader.ReadByte();
                    byte b = reader.ReadByte();
                    result[i] = new Color32(r, g, b, a);
                }
                return result;

            }

            private static Color32[] TryGetRGBA(JSONNode json, MemoryStream memoryStream, int featureDataSTart, int pointsLength)
            {
                JSONNode node = json["RGBA"];
                if (node == null) return null;
                JSONNode offsetNode = node["byteOffset"];
                if (offsetNode == null) return null;
                int colorStart = offsetNode.AsInt;
                memoryStream.Position = featureDataSTart + colorStart;
                var reader = new BinaryReader(memoryStream);

                Color32[] result = new Color32[pointsLength];

                for (int i = 0; i < pointsLength; i++)
                {
                    byte r = reader.ReadByte();
                    byte g = reader.ReadByte();
                    byte b = reader.ReadByte();
                    byte a = reader.ReadByte();
                    result[i] = new Color32(r, g, b, a);
                }
                return result;

            }

            private static Color32[] TryGetRGB565(JSONNode json, MemoryStream memoryStream, int featureDataSTart, int pointsLength)
            {
                JSONNode node = json["RGB565"];
                if (node == null) return null;
                JSONNode offsetNode = node["byteOffset"];
                if (offsetNode == null) return null;
                int colorStart = offsetNode.AsInt;
                memoryStream.Position = featureDataSTart + colorStart;
                var reader = new BinaryReader(memoryStream);

                Color32[] result = new Color32[pointsLength];
                byte a = 255;
                for (int i = 0; i < pointsLength; i++)
                {
                    short data = reader.ReadInt16();
                    byte r = (byte)(data >> 11);
                    byte g = (byte)((data << 5) >> 10);
                    byte b = (byte)((data << 11) >> 11);
                    result[i] = new Color32(r, g, b, a);
                }
                return result;

            }

        }
        public static class RTC_CENTER
        {
            internal static bool TryRead(JSONNode featureTable, out Coordinate RTC_Center)
            {
                JSONNode node = featureTable["RTC_CENTER"];
                if (node == null)
                {
                    RTC_Center = new Coordinate(CoordinateSystem.WGS84_ECEF, 0, 0, 0);
                    return false;
                }
                if (node.Count != 3)
                {
                    RTC_Center = new Coordinate(CoordinateSystem.WGS84_ECEF, 0, 0, 0);
                    return false;
                }

                double x = node[0].AsDouble;
                double y = node[1].AsDouble;
                double z = node[2].AsDouble;
                RTC_Center = new Coordinate(CoordinateSystem.WGS84_ECEF, x, y, z);
                return true;





            }
        }
        public static class Positions
        {
            internal static Vector3[] Read(JSONNode json, MemoryStream memoryStream, int featureDataSTart, int pointsLength)
            {
                JSONNode node = json["POSITION"];
                if (node != null)
                {
                    return GetPosition(json, memoryStream, featureDataSTart, pointsLength);
                }
                node = json["POSITION_QUANTIZED"];
                if (node != null)
                {
                    return GetQuantizedPosition(json, memoryStream, featureDataSTart, pointsLength);
                }

                return null;
            }

            private static Vector3[] GetPosition(JSONNode json, MemoryStream memoryStream, int featureDataSTart, int pointsLength)
            {
                JSONNode node = json["POSITION"];
                int positionStart = -1;
                if (node != null)
                {
                    JSONNode offsetNode = node["byteOffset"];
                    if (offsetNode != null)
                    {
                        positionStart = offsetNode.AsInt;
                    }
                }
                if (positionStart < 0)
                {
                    return null;
                }

                Vector3[] result = new Vector3[pointsLength];
                memoryStream.Position = featureDataSTart + positionStart;
                var reader = new BinaryReader(memoryStream);
                for (int i = 0; i < pointsLength; i++)
                {
                    float x = reader.ReadSingle();
                    float y = reader.ReadSingle();
                    float z = reader.ReadSingle();
                    result[i] = new Vector3(-x, z, -y);
                }
                return result;
            }

            private static Vector3[] GetQuantizedPosition(JSONNode json, MemoryStream memoryStream, int featureDataSTart, int pointsLength)
            {
                JSONNode node = json["POSITION_QUANTIZED"];

                if (node == null) return null;
                JSONNode offsetNode = node["byteOffset"];
                if (offsetNode == null) return null;
                int positionStart = offsetNode.AsInt;


                node = json["QUANTIZED_VOLUME_OFFSET"];
                if (node == null) return null;
                if (node.Count != 3) return null;
                Vector3 offset = new Vector3();
                offset.x = node[0];
                offset.y = node[1];
                offset.z = node[2];


                node = json["QUANTIZED_VOLUME_SCALE"];
                if (node == null) return null;
                if (node.Count != 3) return null;
                Vector3 scale = new Vector3();
                scale.x = node[0].AsFloat / 66565f;
                scale.y = node[1].AsFloat / 66565f;
                scale.z = node[2].AsFloat / 66565f;

                Vector3[] result = new Vector3[pointsLength];
                memoryStream.Position = featureDataSTart + positionStart;
                var reader = new BinaryReader(memoryStream);
                for (int i = 0; i < pointsLength; i++)
                {
                    float x = reader.ReadSingle() * scale.x + offset.x;
                    float y = reader.ReadSingle() * scale.y + offset.y;
                    float z = reader.ReadSingle() * scale.z + offset.x;
                    result[i] = new Vector3(-x, z, -y);
                }
                return result;
            }
        }
    }
}
