using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    [Serializable]
    public partial class GeoJSONPolygonLayer : LayerGameObject
    {
        public List<FeaturePolygonVisualisations> SpawnedVisualisations = new();

        private bool randomizeColorPerFeature = false;
        public bool RandomizeColorPerFeature { get => randomizeColorPerFeature; set => randomizeColorPerFeature = value; }

        [SerializeField] private Material polygonVisualizationMaterial;

        public Material PolygonVisualizationMaterial
        {
            get { return polygonVisualizationMaterial; }
            set
            {
                polygonVisualizationMaterial = value;
                
                foreach (var featureVisualisation in SpawnedVisualisations)
                    featureVisualisation.SetMaterial(value);
            }
        }

        
        public override void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            foreach (var visualization in SpawnedVisualisations)
                visualization.ShowVisualisations(activeInHierarchy);
        }

        public void AddAndVisualizeFeature<T>(Feature feature, CoordinateSystem originalCoordinateSystem)
            where T : GeoJSONObject
        {
            // Skip if feature already exists (comparison is done using hashcode based on geometry)
            if (SpawnedVisualisations.Any(f => f.feature.GetHashCode() == feature.GetHashCode()))
                return;

            // Create visual with random color if enabled
            var featureMaterial = PolygonVisualizationMaterial;
            if (RandomizeColorPerFeature){
                featureMaterial = new Material(PolygonVisualizationMaterial)
                {
                    color = UnityEngine.Random.ColorHSV()
                };
            }

            // Add visualisation to the layer, and store it in the SpawnedVisualisations list where we tie our Feature to the visualisations
            var newFeatureVisualisation = new FeaturePolygonVisualisations { feature = feature };
            if (feature.Geometry is MultiPolygon multiPolygon)
            {
                var polygonVisualisations = GeoJSONGeometryVisualizerUtility.VisualizeMultiPolygon(multiPolygon, originalCoordinateSystem, featureMaterial);
                newFeatureVisualisation.visualisations = polygonVisualisations;
            }
            else if(feature.Geometry is Polygon polygon)
            {
                var singlePolygonVisualisation = GeoJSONGeometryVisualizerUtility.VisualizePolygon(polygon, originalCoordinateSystem, featureMaterial);
                newFeatureVisualisation.visualisations.Append(singlePolygonVisualisation);
            }
            
            SpawnedVisualisations.Add(newFeatureVisualisation);
        }

        public override void DestroyLayer()
        {
            base.DestroyLayer();
            if (Application.isPlaying)
            {
                // Remove all SpawnedVisualisations
                for (int i = SpawnedVisualisations.Count - 1; i >= 0; i--)
                {
                    var featureVisualisation = SpawnedVisualisations[i];
                    RemoveFeature(featureVisualisation);
                }
            }
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
        
        private void RemoveFeature(FeaturePolygonVisualisations featureVisualisation)
        {
            featureVisualisation.DestroyAllVisualisations();
            SpawnedVisualisations.Remove(featureVisualisation);
        }
    }
}