using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    [Serializable]
    public partial class GeoJSONPolygonLayer : LayerGameObject, IGeoJsonVisualisationLayer
    {
        public Transform Transform { get => transform; }

        public List<Mesh> GetMeshData(Feature feature)
        {
            FeaturePolygonVisualisations data = SpawnedVisualisations.Where(f => f.feature == feature).FirstOrDefault();
            List<Mesh> meshes = null;
            if (data != null)
            {
                meshes = new List<Mesh>();
                List<PolygonVisualisation> visualisations = data.Data;
                foreach (PolygonVisualisation polygon in visualisations)
                {
                    meshes.Add(polygon.GetComponent<MeshFilter>().mesh);
                }
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
            //FeaturePolygonVisualisations data = SpawnedVisualisations.Where(f => meshes.Contains(f.Data[0].GetComponent<Mesh>())).FirstOrDefault();
            PolygonVisualisation visualisation = GetPolygonVisualisationByMesh(meshes);
            if(visualisation != null) 
                visualisation.gameObject.GetComponent<MeshRenderer>().material.color = color;
        }

        /// <summary>
        /// not ideal since the polygonvisualisation mesh is not cached. needs caching
        /// returns the polygon visualisation matching the provided meshes
        /// </summary>
        /// <param name="meshes"></param>
        /// <returns></returns>
        public PolygonVisualisation GetPolygonVisualisationByMesh(List<Mesh> meshes)
        {
            foreach (FeaturePolygonVisualisations fpv in SpawnedVisualisations)
            {
                List<PolygonVisualisation> visualisations = fpv.Data;
                foreach (PolygonVisualisation pv in visualisations)
                {
                    if (meshes.Contains(pv.GetComponent<MeshFilter>().mesh))
                    {
                        return pv;
                    }
                }
            }
            return null;
        }

        public void SetVisualisationColorToDefault()
        {
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

        private bool randomizeColorPerFeature = false;
        public bool RandomizeColorPerFeature { get => randomizeColorPerFeature; set => randomizeColorPerFeature = value; }

        [SerializeField] private Material polygonVisualizationMaterial;
        private Material polygonVisualizationMaterialInstance;

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
            Material featureRenderMaterial = GetMaterialInstance();

            // Add visualisation to the layer, and store it in the SpawnedVisualisations list where we tie our Feature to the visualisations
            var newFeatureVisualisation = new FeaturePolygonVisualisations { 
                feature = feature,
                geoJsonPolygonLayer = this
            };
            if (feature.Geometry is MultiPolygon multiPolygon)
            {
                var polygonVisualisations = GeoJSONGeometryVisualizerUtility.VisualizeMultiPolygon(multiPolygon, originalCoordinateSystem, featureRenderMaterial);
                newFeatureVisualisation.AppendVisualisations(polygonVisualisations);
            }
            else if (feature.Geometry is Polygon polygon)
            {
                var singlePolygonVisualisation = GeoJSONGeometryVisualizerUtility.VisualizePolygon(polygon, originalCoordinateSystem, featureRenderMaterial);
                newFeatureVisualisation.AppendVisualisations(singlePolygonVisualisation);
            }

            SpawnedVisualisations.Add(newFeatureVisualisation);
        }

        private Material GetMaterialInstance()
        {
            // Create material with random color if randomize per feature is enabled
            if (RandomizeColorPerFeature)
            {
                var randomColor = UnityEngine.Random.ColorHSV();
                randomColor.a = LayerData.Color.a;

                var featureMaterialInstance = new Material(PolygonVisualizationMaterial) { color = randomColor };
                return featureMaterialInstance;
            }

            // Default to material with layer color
            if (polygonVisualizationMaterialInstance == null)
                    polygonVisualizationMaterialInstance = new Material(PolygonVisualizationMaterial) { color = LayerData.Color };

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