using System;
using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.LayerStyles;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers
{
    [Serializable]
    public partial class GeoJSONPolygonLayer : MonoBehaviour, IGeoJsonGeometryTypeVisualizer
    {
        public string DisplayName => "Polygonen";
        public string StylingColorProperty => Symbolizer.FillColorProperty;

        public bool SupportsGeometryType(Feature feature)
        {
            return feature.Geometry.Type == GeoJSONObjectType.MultiPolygon || feature.Geometry.Type == GeoJSONObjectType.Polygon;
        }

        public int FeatureCount => spawnedVisualisations.Count;

        public Transform Transform => transform;
        public Material RenderMaterial => polygonVisualizationMaterial;

        public event IGeoJsonGeometryTypeVisualizer.GeoJsonHandler FeatureRemoved;

        private Dictionary<Feature, FeaturePolygonVisualisations> spawnedVisualisations = new();

        [SerializeField] private Material polygonVisualizationMaterial;
        [SerializeField] private Material polygonSelectionVisualizationMaterial;
        FeaturePolygonVisualisations selectedVisualisations;
        
        public void SetColorForAllFeatures(Color color)
        {
            var newColor = new Color(color.r, color.g, color.b, polygonVisualizationMaterial.color.a); //todo: we don't support alpha yet in the color picker

            foreach (var visualisation in spawnedVisualisations.Values)
            {
                foreach (var pv in visualisation.Data)
                {
                    pv.SetMaterialPropertyBlockColor(newColor, visualisation != selectedVisualisations);
                }
            }
        }

        public void SetColorForFeature(Feature feature, Color color)
        {
            var newColor = new Color(color.r, color.g, color.b, polygonVisualizationMaterial.color.a); //todo: we don't support alpha yet in the color picker

            var pv = spawnedVisualisations[feature];
            foreach (var polygon in pv.Data)
            {
                polygon.SetMaterialPropertyBlockColor(newColor, pv != selectedVisualisations);
            }
        }
        
        public List<Mesh> GetMeshData(Feature feature)
        {
            FeaturePolygonVisualisations data = spawnedVisualisations[feature];
            List<Mesh> meshes = new List<Mesh>();
            if (data == null) return meshes;

            List<PolygonVisualisation> visualisations = data.Data;
            foreach (PolygonVisualisation polygon in visualisations)
            {
                if (polygon.PolygonMesh == null)
                {
                    Debug.LogError("the polygon visualisation has a missing polygonmesh for feature:" + feature.Id);
                    continue;
                }

                meshes.Add(polygon.PolygonMesh);
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
        public void SetVisualisationSelected(Transform transform, List<Mesh> meshes, Color color)
        {
            foreach (var mesh in meshes)
            {
                var visualisations = GetPolygonVisualisationsByMesh(mesh);
                if (visualisations.pv != null)
                {
                    polygonSelectionVisualizationMaterial.color = color;
                    visualisations.pv.VisualisationMaterial = polygonSelectionVisualizationMaterial;
                    visualisations.pv.SetMaterialPropertyBlockActive(false);
                    
                    selectedVisualisations = visualisations.fpv;
                }
            }
        }
        
        /// <summary>
        /// not ideal since the polygonvisualisation mesh is not cached. needs caching
        /// returns the polygon visualisation matching the provided meshes
        /// </summary>
        /// <param name="meshes"></param>
        /// <returns></returns>
        public (FeaturePolygonVisualisations fpv, PolygonVisualisation pv) GetPolygonVisualisationsByMesh(Mesh mesh)
        {
            foreach (KeyValuePair<Feature, FeaturePolygonVisualisations> fpv in spawnedVisualisations)
            {
                List<PolygonVisualisation> visualisations = fpv.Value.Data;
                foreach (PolygonVisualisation pv in visualisations)
                {
                    if (mesh == pv.PolygonMesh)
                        return (fpv.Value, pv);
                }
            }

            return (null, null);
        }

        public void SetVisualisationDeselected(List<Mesh> meshes)
        {
            foreach (var mesh in meshes)
            {
                var visualisation = GetPolygonVisualisationsByMesh(mesh);
                if (visualisation.pv != null)
                {
                    visualisation.pv.VisualisationMaterial = polygonVisualizationMaterial;
                    visualisation.pv.SetMaterialPropertyBlockActive(true);
                    selectedVisualisations = null;
                }
            }
        }

        public void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            foreach (var visualization in spawnedVisualisations)
            {
                visualization.Value.ShowVisualisations(activeInHierarchy);
            }
        }

        public void AddAndVisualizeFeature(Feature feature, CoordinateSystem originalCoordinateSystem, bool activeInHierarchy)
        {
            // Skip if feature already exists (comparison is done using hashcode based on geometry)
            if (spawnedVisualisations.ContainsKey(feature))
                return;

            var newFeatureVisualisation = new FeaturePolygonVisualisations
            {
                feature = feature,
                geoJsonPolygonLayer = this
            };
            
            // Add visualisation to the layer, and store it in the SpawnedVisualisations list where we tie our Feature
            // to the visualisations
            switch (feature.Geometry)
            {
                case MultiPolygon multiPolygon:
                    newFeatureVisualisation.AppendVisualisations(GeometryVisualizationFactory.CreatePolygonVisualization(
                        multiPolygon,
                        originalCoordinateSystem,
                        polygonVisualizationMaterial
                    ));
                    break;
                case Polygon polygon:
                    newFeatureVisualisation.AppendVisualisations(GeometryVisualizationFactory.CreatePolygonVisualisation(
                        polygon,
                        originalCoordinateSystem,
                        polygonVisualizationMaterial
                    ));
                    break;
            }

            // After setting up the entire visualisation - apply styling so that we use the styling system to tweak
            // this visualisation consistent with what would happen if you re-apply the styling using the ApplyStyling()
            // method
            newFeatureVisualisation.SetMaterial(polygonVisualizationMaterial);
            // bounds are calculated in the AppendVisualisations method, and is therefore not explicitly called here
            spawnedVisualisations.Add(feature, newFeatureVisualisation);
            newFeatureVisualisation.ShowVisualisations(activeInHierarchy);
        }

        /// <summary>
        /// Copy the feature attributes onto the layer feature so that the styling system can
        /// use that as input to pick the correct style.
        /// </summary>
        protected StylingFeature AddAttributesToLayerFeature(StylingFeature feature)
        {
            // it should be a FeaturePolygonVisualisations, just do a sanity check here
            if (feature.Geometry is not FeaturePolygonVisualisations visualisations) return feature;

            foreach (var property in visualisations.feature.Properties)
            {
                feature.Attributes.Add(property.Key, property.Value.ToString());
            }

            return feature;
        }

        private void OnDestroy()
        {
            // Remove all SpawnedVisualisations
            foreach (var kvp in spawnedVisualisations.Reverse())
            {
                RemoveFeature(kvp.Value);
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
            featureVisualisation.Dispose();
            FeatureRemoved?.Invoke(featureVisualisation.feature);
            spawnedVisualisations.Remove(featureVisualisation.feature);
        }

        public BoundingBox GetBoundingBoxOfVisibleFeatures()
        {
            if (spawnedVisualisations.Count == 0)
                return null;

            BoundingBox bbox = null;
            foreach (var vis in spawnedVisualisations.Values)
            {
                if (bbox == null)
                    bbox = new BoundingBox(vis.trueBounds);
                else
                    bbox.Encapsulate(vis.trueBounds);
            }

            var crs2D = CoordinateSystems.To2D(bbox.CoordinateSystem);
            bbox.Convert(crs2D); //remove the height, since a GeoJSON is always 2D. This is needed to make the centering work correctly
            return bbox;
        }
    }
}