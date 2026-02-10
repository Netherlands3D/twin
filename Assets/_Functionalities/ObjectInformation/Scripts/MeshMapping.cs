using Netherlands3D.Coordinates;
using Netherlands3D.SubObjects;
using Netherlands3D.Twin.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Functionalities.ObjectInformation 
{
    /// <summary>
    /// in the future/when production ready, this should probably be renamed to a feature mapping type, as it contains geometry and a boundingbox
    /// </summary>
    public class MeshMapping : IMapping
    {
        public string Id => id;
        public object MappingObject => objectMapping;
        public ObjectMapping ObjectMapping => objectMapping;
        public BoundingBox BoundingBox => boundingBox;
        public List<MeshMappingItem> Items => items;

        private ObjectMapping objectMapping;
        private BoundingBox boundingBox;
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private List<MeshMappingItem> items = null;

        private Vector3[] vertices;
        private int[] triangles;
        private Transform mTransform;

        private string id;
        private Material selectionMaterial;

        public MeshMapping(string id)
        {
            this.id = id;
        }

        public void SetMeshObject(ObjectMapping mapping)
        {
            this.objectMapping = mapping;
            meshRenderer = this.objectMapping.GetComponent<MeshRenderer>();
            meshFilter = this.objectMapping.GetComponent<MeshFilter>();
        }
        
        public void SetSelectionMaterial(Material material)
        {
            this.selectionMaterial = material;
        }

        public MeshMappingItem FindItemForPosition(Vector3 position)
        {
            Coordinate target = new Coordinate(position).Convert(CoordinateSystem.WGS84_LatLon);          
            CacheItems();
            foreach (MeshMappingItem item in items)
            {
                if (item.BoundingBox.Contains(target) && item.IsPositionHit(position, vertices, triangles, mTransform))
                {
                    return item;
                }
            }
            return null;
        }

        public MeshMappingItem FindItemById(string id)
        {
            CacheItems();
            foreach (MeshMappingItem item in items)
            {
                if (item.ObjectMappingItem.objectID == id)
                    return item;
            }
            return null;
        }

        public bool HasItemWithId(string id)
        {         
            return objectMapping.items.ContainsKey(id);
        }

        public void CacheItems()
        {
            if (items == null)
            {
                vertices = meshFilter.sharedMesh.vertices;
                triangles = meshFilter.sharedMesh.triangles;
                mTransform = meshFilter.gameObject.transform;
                items = new List<MeshMappingItem>();
                foreach (ObjectMappingItem item in objectMapping.items.Values)
                {
                    MeshMappingItem mapItem = new MeshMappingItem(item, vertices, mTransform);
                    items.Add(mapItem);
                }
            }
        }

        public Coordinate GetCoordinateForObjectMappingItem(ObjectMapping objectMapping, ObjectMappingItem mapping)
        {
            MeshFilter mFilter = objectMapping.gameObject.GetComponent<MeshFilter>();
            Vector3[] vertices = mFilter.sharedMesh.vertices;
            Vector3 centr = Vector3.zero;
            for (int i = mapping.firstVertex; i < mapping.firstVertex + mapping.verticesLength; i++)
                centr += vertices[i];
            centr /= mapping.verticesLength;

            Vector3 centroidWorld = mFilter.transform.TransformPoint(centr);
            Coordinate coord = new Coordinate(centroidWorld);
            return coord;
        }

        public static Mesh CreateMeshFromMapping(ObjectMapping objectMapping, ObjectMappingItem mapping, out Vector3 localCentroid, bool centerMesh = true)
        {
            var srcTf = objectMapping.gameObject.transform;
            MeshFilter mf = objectMapping.gameObject.GetComponent<MeshFilter>();
            Mesh src = mf.sharedMesh;

            Vector3[] srcV = src.vertices;
            Vector3[] srcN = src.normals;
            int[] srcT = src.triangles;

            int start = mapping.firstVertex;
            int len = mapping.verticesLength;

            // compute centroid in source mesh local space
            localCentroid = Vector3.zero;
            for (int i = 0; i < len; i++)
                localCentroid += srcV[start + i];
            localCentroid /= Mathf.Max(1, len);

            // copy and optionally center vertices
            Vector3[] newV = new Vector3[len];
            Vector3[] newN = (srcN != null && srcN.Length == srcV.Length) ? new Vector3[len] : null;
            for (int i = 0; i < len; i++)
            {
                var v = srcV[start + i];
                newV[i] = (centerMesh ? (v - localCentroid) : v);
                if (newN != null) newN[i] = srcN[start + i];
            }

            // remap triangles that are fully inside the selected vertex range
            var newTris = new List<int>();
            for (int i = 0; i < srcT.Length; i += 3)
            {
                int a = srcT[i], b = srcT[i + 1], c = srcT[i + 2];
                if (a >= start && a < start + len &&
                    b >= start && b < start + len &&
                    c >= start && c < start + len)
                {
                    newTris.Add(a - start);
                    newTris.Add(b - start);
                    newTris.Add(c - start);
                }
            }

            var mesh = new Mesh();
            mesh.vertices = newV;
            if (newN != null) mesh.normals = newN;
            mesh.triangles = newTris.ToArray();
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            return mesh;
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
            
            BoundingBox boundingBox = new BoundingBox(featureBounds);
            boundingBox.Convert(CoordinateSystem.WGS84_LatLon);
            return boundingBox;
        }

        public void DebugBounds(Color color)
        {
            if (items != null)
            {
                foreach (MeshMappingItem item in items)
                {
                    item.BoundingBox.Debug(color);
                }
            }
        }

        public GameObject Select(string subId)
        {
            ObjectMappingItem item = objectMapping.items[subId];
            if(item == null) return null;
            
            GameObject selectedMesh = new GameObject(subId);
            Mesh mesh = CreateMeshFromMapping(ObjectMapping, item, out Vector3 localCentroid);
            MeshFilter mFilter = selectedMesh.AddComponent<MeshFilter>();
            mFilter.mesh = mesh;
            MeshRenderer mRenderer = selectedMesh.AddComponent<MeshRenderer>();
            mRenderer.material = selectionMaterial;
            selectedMesh.transform.position = ObjectMapping.transform.TransformPoint(localCentroid);
            return selectedMesh;
        }

        public void Deselect()
        {
           
        }
    }

    public class MeshMappingItem 
    {
        public BoundingBox BoundingBox => boundingBox;
        public ObjectMappingItem ObjectMappingItem => item;
        private BoundingBox boundingBox;
        private ObjectMappingItem item;
        private const float kEpsilon = 0.000001f;

        public MeshMappingItem(ObjectMappingItem item, Vector3[] vertices, Transform meshTransform)
        {
            this.item = item;
            UpdateBoundingBox(vertices, meshTransform);
        }

        public void UpdateBoundingBox(Vector3[] vertices, Transform meshTransform)
        {
            if (item.firstVertex < 0 || item.firstVertex + item.verticesLength > vertices.Length)
                return;

            //get worldspace bounds
            Vector3 firstVertexWorld = meshTransform.TransformPoint(vertices[item.firstVertex]);
            Vector3 min = firstVertexWorld;
            Vector3 max = firstVertexWorld;

            for (int i = item.firstVertex + 1; i < item.firstVertex + item.verticesLength; i++)
            {
                Vector3 vertexWorld = meshTransform.TransformPoint(vertices[i]);
                min = Vector3.Min(min, vertexWorld); 
                max = Vector3.Max(max, vertexWorld);
            }            
            Bounds bounds = new Bounds();
            bounds.SetMinMax(min, max);            
            boundingBox = new BoundingBox(bounds);
            boundingBox.Convert(CoordinateSystem.WGS84_LatLon);
        }

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

            //start at the first triangle index found and check if the corresponding triangles of this submehs intersect with the camray towards the optical point
            int triMax = item.firstVertex + item.verticesLength;
            for (int j = firstTriangleIndex; j < triangles.Length; j += 3)
            {
                //check tri bounds
                if (triangles[j] >= triMax || triangles[j + 1] >= triMax || triangles[j + 2] >= triMax)
                    break;

                Vector3 v0 = meshTransform.TransformPoint(vertices[triangles[j]]);
                Vector3 v1 = meshTransform.TransformPoint(vertices[triangles[j + 1]]);
                Vector3 v2 = meshTransform.TransformPoint(vertices[triangles[j + 2]]);

                float dist; //TODO we could check based on distance which triangle is closest, but works for now, checking that would be slower
                if (IntersectRayTriangle(camRay, v0, v1, v2, out dist))
                    return true;
            }
            return false;
        }

       
        /// <summary>
        /// if it hits a triangle it returns a length towards the triangle (distance from the ray origin to the hit point)
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="v0"></param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static bool IntersectRayTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out float length)
        {
            // edges from v1 & v2 to v0.     
            Vector3 e1 = v1 - v0;
            Vector3 e2 = v2 - v0;
            length = 0;
            Vector3 h = Vector3.Cross(ray.direction, e2);
            float a = Vector3.Dot(e1, h);
            if ((a > -kEpsilon) && (a < kEpsilon)) return false;            
            float f = 1.0f / a;
            Vector3 s = ray.origin - v0;
            float u = f * Vector3.Dot(s, h);
            if ((u < 0.0f) || (u > 1.0f)) return false;
            Vector3 q = Vector3.Cross(s, e1);
            float v = f * Vector3.Dot(ray.direction, q);
            if ((v < 0.0f) || (u + v > 1.0f)) return false;
            float t = f * Vector3.Dot(e2, q);
            if (t > kEpsilon)
            {
                length = t;
                return true;
            }
            return false;
        }
    }
}