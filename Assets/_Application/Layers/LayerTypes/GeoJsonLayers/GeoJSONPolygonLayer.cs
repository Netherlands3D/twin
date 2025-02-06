using System;
using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.LayerStyles;
using Netherlands3D.SelectionTools;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers
{
    [Serializable]
    public partial class GeoJSONPolygonLayer : LayerGameObject, IGeoJsonVisualisationLayer
    {
        public bool IsPolygon => true;
        public Transform Transform { get => transform; }
        public delegate void GeoJSONPointHandler(Feature feature);
        public event GeoJSONPointHandler FeatureRemoved;

        private Dictionary<Feature, FeaturePolygonVisualisations> spawnedVisualisations = new();     

        public List<Mesh> GetMeshData(Feature feature)
        {
            FeaturePolygonVisualisations data = spawnedVisualisations[feature];
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

        public Bounds GetFeatureBounds(Feature feature)
        {
            return spawnedVisualisations[feature].trueBounds;
        }

        public float GetSelectionRange()
        {
            return 0; //we want to precisely measure the edge to a polygon so no selection range is applied here
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
            foreach (KeyValuePair<Feature, FeaturePolygonVisualisations> fpv in spawnedVisualisations)
            {
                List<PolygonVisualisation> visualisations = fpv.Value.Data;
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
            foreach (KeyValuePair<Feature, FeaturePolygonVisualisations> fpv in spawnedVisualisations)
            {
                List<PolygonVisualisation> visualisations = fpv.Value.Data;
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


        [SerializeField] private Material polygonVisualizationMaterial;
        internal Material polygonVisualizationMaterialInstance;

        public Material PolygonVisualizationMaterial
        {
            get => polygonVisualizationMaterial;
            set
            {
                polygonVisualizationMaterial = value;
                
                foreach (var featureVisualisation in spawnedVisualisations)
                {
                    featureVisualisation.Value.SetMaterial(value);
                }
            }
        }
     
        public override void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            foreach (var visualization in spawnedVisualisations)
            {
                visualization.Value.ShowVisualisations(activeInHierarchy);
            }
        }

        public void AddAndVisualizeFeature<T>(Feature feature, CoordinateSystem originalCoordinateSystem)
            where T : GeoJSONObject
        {
            // Skip if feature already exists (comparison is done using hashcode based on geometry)
            if (spawnedVisualisations.ContainsKey(feature))
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
            spawnedVisualisations.Add(feature, newFeatureVisualisation);
            newFeatureVisualisation.ShowVisualisations(LayerData.ActiveInHierarchy);
        }

        public override void InitializeStyling()
        {
            foreach (var visualisations in spawnedVisualisations)
            {
                visualisations.Value.SetMaterial(GetMaterialInstance());
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
            Debug.Log("Destroying all visualisations " + spawnedVisualisations.Count);
            foreach (var kvp in spawnedVisualisations.Reverse())
            {
                RemoveFeature(kvp.Value);
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
            foreach (var kvp in spawnedVisualisations.Reverse())
            {
                var inCameraFrustum = GeometryUtility.TestPlanesAABB(frustumPlanes, kvp.Value.tiledBounds);
                if (inCameraFrustum)
                    continue;

                RemoveFeature(kvp.Value);
            }
        }
        
        private void RemoveFeature(FeaturePolygonVisualisations featureVisualisation)
        {
            featureVisualisation.DestroyAllVisualisations();
            FeatureRemoved?.Invoke(featureVisualisation.feature); //TODO, fix the execution order, we need to execute this before its removed from the data
            spawnedVisualisations.Remove(featureVisualisation.feature);
        }
    }
}