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
    public class ImportPnts
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


            string keyString = "POINTS_LENGTH";
            JSONNode node;
            JSONNode offsetNode;

            int pointsLength = -1;

            node = featureTable["POINTS_LENGTH"];
            if (node!=null)
            {
                pointsLength = node.AsInt;
            }

            double[] center;
            Coordinate CenterCoordinate = new Coordinate(CoordinateSystem.WGS84_ECEF, 0, 0, 0);
            
            node = featureTable["RTC_CENTER"];
            if (node!=null)
            {
                if (node.Count==3)
                {
                    center = new double[3];
                    double x = node[0].AsDouble;
                    double y = node[1].AsDouble;
                    double z = node[2].AsDouble;
                    CenterCoordinate = new Coordinate(CoordinateSystem.WGS84_ECEF,x,y,z);
                }
            }
            Vector3 unityCenter = CenterCoordinate.ToUnity();
            int positionStart = -1;
            node = featureTable["POSITION"];
            if (node!=null)
            {
                offsetNode = node["byteOffset"];
                if (offsetNode!=null)
                {
                    positionStart = offsetNode.AsInt;
                }
            }

            memoryStream.Seek(featureDataStart + positionStart, SeekOrigin.Begin);

            reader = new BinaryReader(memoryStream);
            Vector3[] verts = new Vector3[pointsLength];
            Coordinate coord = new Coordinate(CoordinateSystem.WGS84_ECEF, 0, 0, 0);
            Coordinate relative;
            int[] indices = new int[pointsLength];
            for (int i = 0; i < pointsLength; i++)
            {
                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                float z = reader.ReadSingle();
                coord.Points[0] = x;
                coord.Points[1] = y;
                coord.Points[2] = z;
                verts[i] = (CenterCoordinate + coord).ToUnity() - unityCenter;

                ;
                indices[i] = i;
            }

            int colorStart = -1;
            node = featureTable["RGB"];
            if (node != null)
            {
                offsetNode = node["byteOffset"];
                if (offsetNode != null)
                {
                    colorStart = offsetNode.AsInt;
                }
            }
            memoryStream.Seek(featureDataStart + colorStart, SeekOrigin.Begin);

            reader = new BinaryReader(memoryStream);
            Color32[] colors = new Color32[pointsLength];
            for (int i = 0; i < pointsLength; i++)
            {
                byte r = reader.ReadByte();
                byte g = reader.ReadByte();
                byte b = reader.ReadByte();
                Color32 color = new Color32(r, g, b, 255);
                
                
                colors[i] = color;
            }


            Mesh mesh = new Mesh();
            mesh.vertices = verts;
            mesh.SetIndices(indices, MeshTopology.Points, 0);
            mesh.colors32 = colors;
            GameObject gameObject = new GameObject();
            gameObject.transform.parent = containerTransform;
            gameObject.transform.position = CenterCoordinate.ToUnity();
            gameObject.transform.rotation = CenterCoordinate.RotationToLocalGravityUp() * gameObject.transform.rotation;
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            Shader shader = Shader.Find("Shader Graphs/Pointcloud");
            
            Material material = new Material(shader);
            meshRenderer.material = material;
            succesCallback.Invoke(true);
        }
    }
}
