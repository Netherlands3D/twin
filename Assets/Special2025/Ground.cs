using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Services;
using Netherlands3D.Twin.Samplers;
using UnityEngine;

namespace Netherlands3D
{
    public class Ground : MonoBehaviour
    {
        private int Width = 4;
        private int Depth = 4;
        private int tileSize = 1000;

        void Start()
        {
            GenerateGround(Camera.main.transform.position);
        }
        
        public void GenerateGround(Vector3 center)
        {
            Vector3 centerPosition = center;
            HeightMap map = ServiceLocator.GetService<HeightMap>();
            Vector3[,] vertices = new Vector3[Width, Depth];

            int halfWidth = Width / 2;
            int halfDepth = Depth / 2;

            for (int x = -halfWidth; x < halfWidth; x++)
            {
                for (int y = -halfDepth; y < halfDepth; y++)
                {
                    Vector3 targetPosition = new Vector3(x * tileSize + centerPosition.x, 0, y * tileSize + centerPosition.z);
                    Coordinate coord = new Coordinate(targetPosition);
                    float height = map.GetHeight(coord);
                    targetPosition.y = height;

                    int ix = x + halfWidth;
                    int iy = y + halfDepth;
                    vertices[ix, iy] = targetPosition;
                }
            }
            List<int> triangles = new List<int>();

            for (int x = 0; x < Width - 1; x++)
            {
                for (int y = 0; y < Depth - 1; y++)
                {
                    int i = y * Width + x;
                    triangles.Add(i);
                    triangles.Add(i + Width);
                    triangles.Add(i + 1);
                    triangles.Add(i + 1);
                    triangles.Add(i + Width);
                    triangles.Add(i + Width + 1);
                }
            }
            Vector3[] meshVertices = new Vector3[Width * Depth];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Depth; y++)
                {
                    meshVertices[y * Width + x] = vertices[x, y];
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = meshVertices;
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();

            MeshCollider collider = gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = false;
            
            // MeshFilter mf = gameObject.AddComponent<MeshFilter>();
            // mf.mesh = mesh;
            //
            // MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();
            // mr.material = new Material(Shader.Find("Standard")); 
        }
    }
}
