using Netherlands3D.Coordinates;
using Netherlands3D.SubObjects;
using Netherlands3D.Twin.Utility;
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace Netherlands3D.Functionalities.ObjectInformation 
{
    /// <summary>
    /// in the future/when production ready, this should probably be renamed to a feature mapping type, as it contains geometry and a boundingbox
    /// </summary>
    public class MeshMapping : MonoBehaviour, IMapping
    {
        public ObjectMapping ObjectMapping => objectMapping;
        public BoundingBox BoundingBox => boundingBox;
        public List<MeshMappingItem> Items => items;

        private ObjectMapping objectMapping;
        private BoundingBox boundingBox;
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private List<MeshMappingItem> items = null;

        public void SetMeshObject(ObjectMapping mapping)
        {
            this.objectMapping = mapping;
            meshRenderer = this.objectMapping.GetComponent<MeshRenderer>();
            meshFilter = this.objectMapping.GetComponent<MeshFilter>();
        }

        public MeshMappingItem FindItemForPosition(Vector3 position)
        {
            Coordinate target = new Coordinate(CoordinateSystem.Unity, position.x, position.y, position.z).Convert(CoordinateSystem.WGS84_LatLon);
            Vector3[] vertices = meshFilter.sharedMesh.vertices;
            int[] triangles = meshFilter.sharedMesh.triangles;
            Transform mTransform = meshFilter.gameObject.transform;
            if (items == null)
            {
                items = new List<MeshMappingItem>();
                foreach (ObjectMappingItem item in objectMapping.items)
                {
                    MeshMappingItem mapItem = new MeshMappingItem(item, vertices, mTransform);
                    items.Add(mapItem);
                }
            }           
            foreach (MeshMappingItem item in items)
            {
                if (item.BoundingBox.Contains(target) && item.IsPositionHit(position, vertices, triangles, mTransform))
                {
                    return item;
                }
            }
            return null;
        }

        //maybe this should be automated and called within the set visualisation layer
        public void UpdateBoundingBox()
        {
            if (ObjectMapping == null)
            {
                Debug.LogError("must have feature for boundingbox");
                return;
            }
            if (meshRenderer == null)
            {
                Debug.LogError("must have a renderer to determine the boundingbox");
                return;
            }
            boundingBox = CreateBoundingBoxForMesh(ObjectMapping, meshRenderer);
        }

        public static BoundingBox CreateBoundingBoxForMesh(ObjectMapping mapping, MeshRenderer renderer)
        {
            Bounds featureBounds = renderer.bounds;
            Coordinate bottomLeft = new Coordinate(CoordinateSystem.Unity, featureBounds.min.x, featureBounds.min.y, featureBounds.min.z);
            Coordinate topRight = new Coordinate(CoordinateSystem.Unity, featureBounds.max.x, featureBounds.max.y, featureBounds.max.z);
            Coordinate blWgs84 = bottomLeft.Convert(CoordinateSystem.WGS84_LatLon);
            Coordinate trWgs84 = topRight.Convert(CoordinateSystem.WGS84_LatLon);
            BoundingBox boundingBox = new(blWgs84, trWgs84);
            return boundingBox;
        }

        public void RemoveItems()
        {

        }

        void OnDrawGizmos()
        {
            if(items != null)
            {
                foreach (MeshMappingItem item in items)
                {
                    item.BoundingBox.Debug(Color.blue);
                    item.Debug(Color.red);
                }
            }
        }
    }

    public class MeshMappingItem : IMapping
    {
        public BoundingBox BoundingBox => boundingBox;
        public ObjectMappingItem ObjectMappingItem => item;
        private BoundingBox boundingBox;
        private ObjectMappingItem item;

        public MeshMappingItem(ObjectMappingItem item, Vector3[] vertices, Transform meshTransform)
        {
            this.item = item;
            UpdateBoundingBox(vertices, meshTransform);
        }

        public void UpdateBoundingBox(Vector3[] vertices, Transform meshTransform)
        {
            if (item.firstVertex < 0 || item.firstVertex + item.verticesLength > vertices.Length)
                return;

            // Initialize min and max as the first vertex (in world space)
            Vector3 firstVertexWorld = meshTransform.TransformPoint(vertices[item.firstVertex]);
            Vector3 min = firstVertexWorld;
            Vector3 max = firstVertexWorld;

            // Iterate through the vertices and update min/max bounds
            for (int i = item.firstVertex + 1; i < item.firstVertex + item.verticesLength; i++)
            {
                Vector3 vertexWorld = meshTransform.TransformPoint(vertices[i]);
                min = Vector3.Min(min, vertexWorld);  // Update min
                max = Vector3.Max(max, vertexWorld);  // Update max
            }

            // Create the bounding box from the world-space min/max
            Bounds bounds = new Bounds();
            bounds.SetMinMax(min, max);  // Sets the min and max based on the world space vertices

            // Convert to coordinate system as needed
            Coordinate bottomLeft = new Coordinate(CoordinateSystem.Unity, bounds.min.x, bounds.min.y, bounds.min.z);
            Coordinate topRight = new Coordinate(CoordinateSystem.Unity, bounds.max.x, bounds.max.y, bounds.max.z);
            Coordinate blWgs84 = bottomLeft.Convert(CoordinateSystem.WGS84_LatLon);
            Coordinate trWgs84 = topRight.Convert(CoordinateSystem.WGS84_LatLon);
            boundingBox = new BoundingBox(blWgs84, trWgs84);
        }

        private Vector3[] testVertices = new Vector3[4];
        public bool IsPositionHit(Vector3 worldPosition, Vector3[] vertices, int[] triangles, Transform meshTransform)
        {            
            Vector3 localPosition = meshTransform.InverseTransformPoint(worldPosition);            
            Vector3 dir = worldPosition - Camera.main.transform.position;

            //shoot towards optical point
            Ray camRay = new Ray(Camera.main.transform.position, dir); 

            //get valid traingle index
            int firstTriangleIndex = -1;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                if (triangles[i] >= item.firstVertex && triangles[i] < item.firstVertex + item.verticesLength)
                {
                    firstTriangleIndex = i;
                    break;
                }
            }
            if (firstTriangleIndex == -1) return false;

            //debugSubmeshVertices.Clear();
            int triMax = item.firstVertex + item.verticesLength;
            for (int j = firstTriangleIndex; j < triangles.Length; j += 3)
            {
                //check tri bounds
                if (triangles[j] >= triMax || triangles[j + 1] >= triMax || triangles[j + 2] >= triMax)
                    break;

                Vector3 v0 = meshTransform.TransformPoint(vertices[triangles[j]]);
                Vector3 v1 = meshTransform.TransformPoint(vertices[triangles[j + 1]]);
                Vector3 v2 = meshTransform.TransformPoint(vertices[triangles[j + 2]]);

                //debugSubmeshVertices.Add(v0);
                //debugSubmeshVertices.Add(v1);
                //debugSubmeshVertices.Add(v2);

                float dist;
                if (IntersectRayTriangle(camRay, v0, v1, v2, out dist))
                {
                    testVertices[0] = v0;
                    testVertices[1] = v1;
                    testVertices[2] = v2;
                    testVertices[3] = worldPosition;
                    return true;
                }
            }
            return false;
        }

        private const float kEpsilon = 0.000001f;
        /// <returns><c>The distance along the ray to the intersection</c> if one exists, <c>NaN</c> if one does not.</returns>
        /// <param name="ray">Le ray.</param>
        /// <param name="v0">A vertex of the triangle.</param>
        /// <param name="v1">A vertex of the triangle.</param>
        /// <param name="v2">A vertex of the triangle.</param>
        public static bool IntersectRayTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out float length)
        {
            // edges from v1 & v2 to v0.     
            Vector3 e1 = v1 - v0;
            Vector3 e2 = v2 - v0;
            length = 0;
            Vector3 h = Vector3.Cross(ray.direction, e2);
            float a = Vector3.Dot(e1, h);
            if ((a > -kEpsilon) && (a < kEpsilon))
            {
                return false;
            }

            float f = 1.0f / a;

            Vector3 s = ray.origin - v0;
            float u = f * Vector3.Dot(s, h);
            if ((u < 0.0f) || (u > 1.0f))
            {
                return false;
            }

            Vector3 q = Vector3.Cross(s, e1);
            float v = f * Vector3.Dot(ray.direction, q);
            if ((v < 0.0f) || (u + v > 1.0f))
            {
                return false;
            }

            float t = f * Vector3.Dot(e2, q);
            if (t > kEpsilon)
            {
                length = t;
                return true;
            }
            else
            {
                return false;
            }
        }
        private List<Vector3> debugSubmeshVertices = new List<Vector3>();

        public void Debug(Color color)
        {

            for (int i = 0; i < testVertices.Length; i++)
            {
                testVertices[i].y = 50;
            }
            //UnityEngine.Debug.DrawLine(testVertices[0], testVertices[1], color);
            //UnityEngine.Debug.DrawLine(testVertices[1], testVertices[2], color);
            //UnityEngine.Debug.DrawLine(testVertices[2], testVertices[0], color);

            //UnityEngine.Debug.DrawLine(testVertices[0], testVertices[3], color);
            //UnityEngine.Debug.DrawLine(testVertices[1], testVertices[3], color);
            //UnityEngine.Debug.DrawLine(testVertices[2], testVertices[3], color);

            for (int i = 0; i < debugSubmeshVertices.Count - 1; i++)
            {
               UnityEngine.Debug.DrawLine(debugSubmeshVertices[i], debugSubmeshVertices[i + 1], color);
            }
        }

        //private bool IsPointInTriangle(Vector3 p, Vector3 v0, Vector3 v1, Vector3 v2)
        //{
        //    // Calculate vectors from point p to vertices v0, v1, v2
        //    Vector3 v0v1 = v1 - v0;
        //    Vector3 v0v2 = v2 - v0;
        //    Vector3 v0p = p - v0;

        //    float dot00 = Vector3.Dot(v0v1, v0v1);
        //    float dot01 = Vector3.Dot(v0v1, v0v2);
        //    float dot02 = Vector3.Dot(v0v1, v0p);
        //    float dot11 = Vector3.Dot(v0v2, v0v2);
        //    float dot12 = Vector3.Dot(v0v2, v0p);

        //    float invDenom = 1f / (dot00 * dot11 - dot01 * dot01);

        //    float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        //    float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        //    // Tolerance for floating-point precision errors (can be adjusted)
        //    const float tolerance = 1e-6f;

        //    // Check if the point is inside the triangle
        //    return (u >= -tolerance) && (v >= -tolerance) && (u + v <= 1 + tolerance);
        //}
    }
}