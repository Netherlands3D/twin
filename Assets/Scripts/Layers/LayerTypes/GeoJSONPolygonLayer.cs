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
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    [Serializable]
    public partial class GeoJSONPolygonLayer : LayerGameObject, IGeoJsonVisualisationLayer
    {
        public bool IsPolygon => true;
        public Transform Transform { get => transform; }

        public List<Mesh> GetMeshData(Feature feature)
        {
            FeaturePolygonVisualisations data = SpawnedVisualisations.Where(f => f.feature == feature).FirstOrDefault();
            List<Mesh> meshes = new List<Mesh>();
            if (data == null) return meshes;

            List<PolygonVisualisation> visualisations = data.Data;
            foreach (PolygonVisualisation polygon in visualisations)
            {
                //TODO would really like to have the meshfilter or mesh cached within the polygonvisualisation (in external package)
                meshes.Add(polygon.GetComponent<MeshFilter>().mesh);
            }

            return meshes;
        }

        /// <summary>
        /// set the colors for the polygon visualisation within the feature polygon visualisation matching the meshes provided
        /// </summary>
        /// <param name="meshes"></param>
        /// <param name="vertexColors"></param>
        public void SetVisualisationColor(Transform transform, List<Mesh> meshes, Color color)
        {
            //TODO would really like to have the meshrenderer cached within the polygonvisualisation (in external package)
            PolygonVisualisation visualisation = GetPolygonVisualisationByMesh(meshes);
            if(visualisation != null)
            {
                visualisation.gameObject.GetComponent<MeshRenderer>().material.color = color;
            }
        }

        /// <summary>
        /// not ideal since the polygonvisualisation mesh is not cached. needs caching
        /// returns the polygon visualisation matching the provided meshes
        /// </summary>
        /// <param name="meshes"></param>
        /// <returns></returns>
        public PolygonVisualisation GetPolygonVisualisationByMesh(List<Mesh> meshes)
        {
            //TODO would really like to have the meshrenderer cached within the polygonvisualisation (in external package)
            foreach (FeaturePolygonVisualisations fpv in SpawnedVisualisations)
            {
                List<PolygonVisualisation> visualisations = fpv.Data;
                foreach (PolygonVisualisation pv in visualisations)
                {
                    if (!meshes.Contains(pv.GetComponent<MeshFilter>().mesh)) continue;
    
                    return pv;
                }
            }
            return null;
        }

        public void SetVisualisationColorToDefault()
        {
            //TODO would really like to have the meshrenderer cached within the polygonvisualisation (in external package)
            Color defaultColor = GetRenderColor();
            foreach (FeaturePolygonVisualisations fpv in SpawnedVisualisations)
            {
                List<PolygonVisualisation> visualisations = fpv.Data;
                foreach (PolygonVisualisation pv in visualisations)
                {
                    if (pv != null)
                        pv.gameObject.GetComponent<MeshRenderer>().material.color = defaultColor;
                }
            }
        }

        public Color GetRenderColor()
        {
            return polygonVisualizationMaterialInstance.color;
        }

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

            // bounds are calculated in the AppendVisualisations method, and is therefore not explicitly called here
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