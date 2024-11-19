using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.LayerStyles;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    [Serializable]
    public partial class GeoJSONPolygonLayer : LayerGameObject
    {
        public List<FeaturePolygonVisualisations> SpawnedVisualisations = new();

        [SerializeField] private Material polygonVisualizationMaterial;
        internal Material polygonVisualizationMaterialInstance;

        public Material PolygonVisualizationMaterial
        {
            get => polygonVisualizationMaterial;
            set
            {
                polygonVisualizationMaterial = value;
                
                foreach (var featureVisualisation in SpawnedVisualisations)
                {
                    featureVisualisation.SetMaterial(value);
                }
            }
        }
     
        public override void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            foreach (var visualization in SpawnedVisualisations)
            {
                visualization.ShowVisualisations(activeInHierarchy);
            }
        }

        public void AddAndVisualizeFeature<T>(Feature feature, CoordinateSystem originalCoordinateSystem)
            where T : GeoJSONObject
        {
            // Skip if feature already exists (comparison is done using hashcode based on geometry)
            if (SpawnedVisualisations.Any(f => f.feature.GetHashCode() == feature.GetHashCode()))
                return;

            var newFeatureVisualisation = new FeaturePolygonVisualisations { 
                feature = feature,
                geoJsonPolygonLayer = this
            };
            Material featureRenderMaterial = GetMaterialInstance();

            // Add visualisation to the layer, and store it in the SpawnedVisualisations list where we tie our Feature
            // to the visualisations
            switch (feature.Geometry)
            {
                case MultiPolygon multiPolygon:
                    newFeatureVisualisation.AppendVisualisations(GeometryVisualizationFactory.CreatePolygonVisualization(
                        multiPolygon, 
                        originalCoordinateSystem, 
                        featureRenderMaterial
                    ));
                    break;
                case Polygon polygon:
                    newFeatureVisualisation.AppendVisualisations(GeometryVisualizationFactory.CreatePolygonVisualisation(
                        polygon, 
                        originalCoordinateSystem, 
                        featureRenderMaterial
                    ));
                    break;
            }

            SpawnedVisualisations.Add(newFeatureVisualisation);
            newFeatureVisualisation.ShowVisualisations(LayerData.ActiveInHierarchy);
        }

        public void ApplyStyling()
        {
            foreach (var visualisations in SpawnedVisualisations)
            {
                visualisations.SetMaterial(GetMaterialInstance());
            }
        }

        private Material GetMaterialInstance()
        {
            if (!polygonVisualizationMaterialInstance)
            {
                polygonVisualizationMaterialInstance = new Material(PolygonVisualizationMaterial)
                {
                    color = LayerData.DefaultSymbolizer.GetFillColor() ?? Color.white
                };
            }

            return polygonVisualizationMaterialInstance;
        }

        public override void DestroyLayerGameObject()
        {
            // Remove all SpawnedVisualisations
            Debug.Log("Destroying all visualisations " + SpawnedVisualisations.Count);  
            for (int i = SpawnedVisualisations.Count - 1; i >= 0; i--)
            {
                var featureVisualisation = SpawnedVisualisations[i];
                RemoveFeature(featureVisualisation);
            }

            base.DestroyLayerGameObject();
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