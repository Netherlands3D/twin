using System;
using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.LayerStyles;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers
{
    [Serializable]
    public partial class GeoJSONPointLayer : LayerGameObject, IGeoJsonVisualisationLayer
    {
        [SerializeField] private BatchedMeshInstanceRenderer pointRenderer3D;
        public bool IsPolygon => false;

        public Transform Transform => transform;
        private Dictionary<int, FeaturePointVisualisations> spawnedVisualisationDictionary = new();

        public List<Mesh> GetMeshData(Feature feature)
        {
            //FeaturePointVisualisations data = SpawnedVisualisations.FirstOrDefault(f => f.feature == feature);
            FeaturePointVisualisations data = spawnedVisualisationDictionary[feature.GetHashCode()];
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
            }

            return meshes;
        }

        //here we have to local offset the vertices with the position of the transform because the transform gets shifted
        public void SetVisualisationColor(Transform transform, List<Mesh> meshes, Color color)
        {
            foreach (Mesh mesh in meshes)
            {
                Vector3[] vertices = mesh.vertices;
                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector3 localOffset = vertices[i] - mesh.bounds.center;
                    pointRenderer3D.SetLineColorClosestToPoint(transform.position + localOffset, color);
                }
            }
        }

        public void SetVisualisationColorToDefault()
        {
            PointRenderer3D.SetDefaultColors();
        }

        public Color GetRenderColor()
        {
            return pointRenderer3D.Material.color;
        }
        
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
            if (spawnedVisualisationDictionary.Keys.Contains(feature.GetHashCode()))
                return;

            var newFeatureVisualisation = new FeaturePointVisualisations { feature = feature };
            ApplyStyling();

            if (feature.Geometry is MultiPoint multiPoint)
            {
                var newPointCollection = GeometryVisualizationFactory.CreatePointVisualisation(multiPoint, originalCoordinateSystem, PointRenderer3D);
                newFeatureVisualisation.Data.Add(newPointCollection);
            }
            else if (feature.Geometry is Point point)
            {
                var newPointCollection = GeometryVisualizationFactory.CreatePointVisualization(point, originalCoordinateSystem, PointRenderer3D);
                newFeatureVisualisation.Data.Add(newPointCollection);
            }
            
            newFeatureVisualisation.CalculateBounds();
            spawnedVisualisationDictionary.Add(feature.GetHashCode(), newFeatureVisualisation);
        }

        public override void InitializeStyling()
        {
            pointRenderer3D.Material = GetMaterialInstance();
        }

        public void ApplyStyling()
        {
            // Currently we don't apply individual styling per feature
        }

        private Material GetMaterialInstance()
        {
            return new Material(pointRenderer3D.Material)
            {
                color = LayerData.DefaultSymbolizer?.GetFillColor() ?? Color.white
            };
        }

        /// <summary>
        /// Checks the Bounds of the visualisations and checks them against the camera frustum
        /// to remove visualisations that are out of view
        /// </summary>
        private List<int> keysToRemove = new List<int>();
        public void RemoveFeaturesOutOfView()
        {
            // Remove visualisations that are out of view
            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            
            keysToRemove.Capacity = spawnedVisualisationDictionary.Count;
            foreach (var kvp in spawnedVisualisationDictionary)
            {
                var visualisation = kvp.Value;
                var inCameraFrustum = GeometryUtility.TestPlanesAABB(frustumPlanes, visualisation.bounds);
                if (inCameraFrustum)
                    continue;

                keysToRemove.Add(kvp.Key);
            }
            foreach (int key in keysToRemove)
                RemoveFeature(key);

            keysToRemove.Clear();
        }

        private void RemoveFeature(int featureVisualisationKey)
        {
            foreach (var pointCollection in spawnedVisualisationDictionary[featureVisualisationKey].Data)
                PointRenderer3D.RemoveCollection(pointCollection);

            spawnedVisualisationDictionary.Remove(featureVisualisationKey);
        }

        public override void DestroyLayerGameObject()
        {
            if (Application.isPlaying && PointRenderer3D && PointRenderer3D.gameObject)
                GameObject.Destroy(PointRenderer3D.gameObject);

            base.DestroyLayerGameObject();
        }
    }
}