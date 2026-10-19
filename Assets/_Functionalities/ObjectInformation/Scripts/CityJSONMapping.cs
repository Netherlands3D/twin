using System.Collections.Generic;
using Netherlands3D.CityJson.Structure;
using Netherlands3D.CityJson.Visualisation;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Functionalities.ObjectInformation
{
    public class CityJSONMapping : IMapping
    {
        public object MappingObject => feature;
        public string Id => feature.Id;
        public LayerData LayerData => VisualisationParent.LayerData;
        
        public CityJSONLayerGameObject VisualisationParent  { get { return visualisationParent; } }
        public CityObject Feature { get { return feature; } }
        public CityObjectMeshVisualizer Visualizer => visualizer;

        private CityObject feature;
        private List<Mesh> meshes;
        private BoundingBox boundingBox;
        private CityJSONLayerGameObject visualisationParent;
        private CityObjectMeshVisualizer visualizer;
        private const float kEpsilon = 0.000001f;

        public BoundingBox BoundingBox
        {
            get
            {
                if(boundingBox == null)
                    UpdateBoundingBox();
                return boundingBox;
            }
        }
        
        public bool IsPositionHit(Vector3 worldPosition)
        {
            var cam = App.Cameras.ActiveCamera;
            Ray camRay = new Ray(
                cam.transform.position,
                worldPosition - cam.transform.position
            );

            UpdateVisualizer();
            Transform meshTransform = visualizer.MeshFilter.gameObject.transform;
            
            foreach (Mesh mesh in meshes)
            {
                Vector3[] vertices = mesh.vertices;
                int[] triangles = mesh.triangles;

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 v0 = meshTransform.TransformPoint(vertices[triangles[i]]);
                    Vector3 v1 = meshTransform.TransformPoint(vertices[triangles[i + 1]]);
                    Vector3 v2 = meshTransform.TransformPoint(vertices[triangles[i + 2]]);

                    if (IntersectRayTriangle(camRay, v0, v1, v2, out float dist))
                        return true;
                }
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

        public void UpdateBoundingBox()
        {
            Vector3Double MinExtent = feature.MinExtent;
            Vector3Double MaxExtent = feature.MaxExtent;
            Coordinate bottomLeft = new Coordinate(feature.CoordinateSystem, MinExtent.x, MinExtent.y, MinExtent.z);
            Coordinate topRight = new Coordinate(feature.CoordinateSystem, MaxExtent.x, MaxExtent.y, MaxExtent.z);
            boundingBox = new BoundingBox(bottomLeft, topRight );    
        }
        
        public void SetCityJsonLayerParent(CityJSONLayerGameObject parentLayer)
        {
            visualisationParent = parentLayer;
        }

        public void SetFeature(CityObject feature)
        {
            this.feature = feature; 
        }

        private void UpdateVisualizer()
        {
            if (visualizer == null)
            {
                visualizer = feature.GetComponent<CityObjectMeshVisualizer>();
                meshes = visualizer.GetAllMeshes();
            }
        }
        
        public void Select(string subId = null)
        {
            UpdateVisualizer();
            foreach (var mesh in meshes)
            {
                Color col = new Color(1, 0, 0, 0);
                List<Color> colors = new List<Color>();
                foreach (Vector3 v in mesh.vertices)
                    colors.Add(col);

                mesh.SetColors(colors);
            }
        }

        public void Deselect()
        {
            if(meshes == null || meshes.Count == 0) return;
            
            foreach (var mesh in meshes)
            {
                Color col = new Color(0, 0, 1, 0);
                List<Color> colors = new List<Color>();
                foreach (Vector3 v in mesh.vertices)
                    colors.Add(col);

                mesh.SetColors(colors);
            }
        }

        public void Debug()
        {
            boundingBox.Debug(Color.blueViolet);
        }
    }
}
