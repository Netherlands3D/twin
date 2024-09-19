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
    public partial class GeoJSONPointLayer : LayerGameObject
    {
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
                newFeatureVisualisation.pointCollection.Add(newPointCollection);
            }
            else if(feature.Geometry is Point point)
            {
                var newPointCollection = GeoJSONGeometryVisualizerUtility.VisualizePoint(point, originalCoordinateSystem, PointRenderer3D);
                newFeatureVisualisation.pointCollection.Add(newPointCollection);
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
            foreach(var pointCollection in featureVisualisation.pointCollection)
                PointRenderer3D.RemoveCollection(pointCollection);

            SpawnedVisualisations.Remove(featureVisualisation);
        }

        public override void DestroyLayerGameObject()
        {
            if (Application.isPlaying && PointRenderer3D && PointRenderer3D.gameObject)
                GameObject.Destroy(PointRenderer3D.gameObject);
                
            base.DestroyLayerGameObject();
        }
    }
}