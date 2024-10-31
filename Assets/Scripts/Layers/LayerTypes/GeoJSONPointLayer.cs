using System;
using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    [Serializable]
    public partial class GeoJSONPointLayer : LayerGameObject, IGeoJsonVisualisationLayer
    {
        public List<Mesh> GetMeshData(Feature feature)
        {
            FeaturePointVisualisations data = SpawnedVisualisations.Where(f => f.feature == feature).FirstOrDefault();
            List<Mesh> meshes = new List<Mesh>();
            foreach (List<Coordinate> points in data.Data)
            {
                Mesh mesh = new Mesh();
                meshes.Add(mesh);
                List<Vector3> vertices = new List<Vector3>();
                foreach (Coordinate point in points)
                {
                    vertices.Add(point.ToUnity());
                }
                mesh.SetVertices(vertices);
                if (points.Count < 3)              
                    continue; 
                int[] triangles = new int[(points.Count - 2) * 3];
                for (int i = 0; i < points.Count - 2; i++)
                {
                    triangles[i * 3] = 0;
                    triangles[i * 3 + 1] = i + 1;
                    triangles[i * 3 + 2] = i + 2;
                }
                mesh.SetTriangles(triangles, 0);
            }
            return meshes;
        }

        public List<FeaturePointVisualisations> SpawnedVisualisations = new();

        private bool randomizeColorPerFeature = false;
        public bool RandomizeColorPerFeature { get => randomizeColorPerFeature; set => randomizeColorPerFeature = value; }

        [SerializeField] private BatchedMeshInstanceRenderer pointRenderer3D;

        public BatchedMeshInstanceRenderer PointRenderer3D
        {
            get { return pointRenderer3D; }
            set
            {
                //todo: move old lines to new renderer, remove old lines from old renderer without clearing entire list?
                // value.SetPositionCollections(pointRenderer3D.PositionCollections); 
                // Destroy(pointRenderer3D.gameObject);
                pointRenderer3D = value;
            }
        }

        public override void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            pointRenderer3D.gameObject.SetActive(activeInHierarchy);
        }

        public void AddAndVisualizeFeature<T>(Feature feature, CoordinateSystem originalCoordinateSystem)
            where T : GeoJSONObject
        {
            // Skip if feature already exists (comparison is done using hashcode based on geometry)
            if (SpawnedVisualisations.Any(f => f.feature.GetHashCode() == feature.GetHashCode()))
                return;

            var newFeatureVisualisation = new FeaturePointVisualisations() { feature = feature };

            // Create visual with random color if enabled
            pointRenderer3D.Material = GetMaterialInstance();

            if (feature.Geometry is MultiPoint multiPoint)
            {
                var newPointCollection = GeoJSONGeometryVisualizerUtility.VisualizeMultiPoint(multiPoint, originalCoordinateSystem, PointRenderer3D);
                newFeatureVisualisation.Data.Add(newPointCollection);
            }
            else if(feature.Geometry is Point point)
            {
                var newPointCollection = GeoJSONGeometryVisualizerUtility.VisualizePoint(point, originalCoordinateSystem, PointRenderer3D);
                newFeatureVisualisation.Data.Add(newPointCollection);
            }

            SpawnedVisualisations.Add(newFeatureVisualisation);
        }

        private Material GetMaterialInstance()
        {
            Material featureMaterialInstance;
            // Create material with random color if randomize per feature is enabled
            if (RandomizeColorPerFeature)
            {
                var randomColor = UnityEngine.Random.ColorHSV();
                randomColor.a = LayerData.Color.a;

                featureMaterialInstance = new Material(pointRenderer3D.Material) { color = randomColor };
                return featureMaterialInstance;
            }

            // Default to material with layer color
            featureMaterialInstance = new Material(pointRenderer3D.Material) { color = LayerData.Color };
            return featureMaterialInstance;
        }

        /// <summary>
        /// Checks the Bounds of the visualisations and checks them against the camera frustum
        /// to remove visualisations that are out of view
        /// </summary>
        public void RemoveFeaturesOutOfView()
        {         
            // Remove visualisations that are out of view
            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            for (int i = SpawnedVisualisations.Count - 1; i >= 0 ; i--)
            {
                // Make sure to recalculate bounds because they can change due to shifts
                SpawnedVisualisations[i].CalculateBounds();

                var inCameraFrustum = GeometryUtility.TestPlanesAABB(frustumPlanes, SpawnedVisualisations[i].bounds);
                if (inCameraFrustum)
                    continue;

                var featureVisualisation = SpawnedVisualisations[i];
                RemoveFeature(featureVisualisation);
            }
        }
        
        private void RemoveFeature(FeaturePointVisualisations featureVisualisation)
        {
            foreach(var pointCollection in featureVisualisation.Data)
                PointRenderer3D.RemoveCollection(pointCollection);

            SpawnedVisualisations.Remove(featureVisualisation);
        }

        public override void DestroyLayerGameObject()
        {
            if (Application.isPlaying && PointRenderer3D && PointRenderer3D.gameObject)
                GameObject.Destroy(PointRenderer3D.gameObject);
                
            base.DestroyLayerGameObject();
        }

        public void SetVisualisationColor(List<Mesh> meshes, Color color)
        {
            foreach (Mesh mesh in meshes)
            {
                Vector3[] vertices = mesh.vertices;
                for (int i = 0; i < vertices.Length; i++)
                {
                    pointRenderer3D.SetLineColorClosestToPoint(vertices[i], color);
                }
            }
        }

        public Color GetRenderColor()
        {
            return pointRenderer3D.Material.color;
        }
    }
}