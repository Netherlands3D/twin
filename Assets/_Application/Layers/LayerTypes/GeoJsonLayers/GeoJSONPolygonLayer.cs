using System;
using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.LayerStyles;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers
{
    [Serializable]
    public partial class GeoJSONPolygonLayer : MonoBehaviour, IGeoJsonVisualisationLayer
    {
        private GeoJsonLayerGameObject parentLayerVisualization;

        public bool IsPolygon => true;

        public Transform Transform
        {
            get => transform;
        }

        public event IGeoJsonVisualisationLayer.GeoJsonHandler FeatureRemoved;

        private Dictionary<Feature, FeaturePolygonVisualisations> spawnedVisualisations = new();

        [SerializeField] private Material polygonVisualizationMaterial;

        internal Material polygonVisualizationMaterialInstance;
        [SerializeField] private Material polygonSelectionVisualizationMaterial;

        public Color RenderColor
        {
            get
            {
                return polygonVisualizationMaterialInstance.color;
            }
            set
            {
                if (polygonVisualizationMaterialInstance == null)
                    polygonVisualizationMaterialInstance = new Material(polygonVisualizationMaterial);
                polygonVisualizationMaterialInstance.color = value;
                //todo: we currently only support coloring the entire layer, if we want to support per feature coloring, this should be changed to a function with a feature as a parameter
                foreach (var visualisation in spawnedVisualisations.Values)
                    visualisation.SetMaterial(polygonVisualizationMaterialInstance);
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
                PolygonVisualisation visualisation = GetPolygonVisualisationByMesh(mesh);
                if (visualisation != null)
                {
                    visualisation.VisualisationMaterial = polygonSelectionVisualizationMaterial;

                    Color col = new Color(1, 0, 0, 0);
                    List<Color> colors = new List<Color>();
                    foreach (Vector3 v in visualisation.PolygonMesh.vertices)
                        colors.Add(col);

                    visualisation.PolygonMesh.SetColors(colors);
                }
            }
        }

        /// <summary>
        /// not ideal since the polygonvisualisation mesh is not cached. needs caching
        /// returns the polygon visualisation matching the provided meshes
        /// </summary>
        /// <param name="meshes"></param>
        /// <returns></returns>
        public PolygonVisualisation GetPolygonVisualisationByMesh(Mesh mesh)
        {
            foreach (KeyValuePair<Feature, FeaturePolygonVisualisations> fpv in spawnedVisualisations)
            {
                List<PolygonVisualisation> visualisations = fpv.Value.Data;
                foreach (PolygonVisualisation pv in visualisations)
                {
                    if (mesh == pv.PolygonMesh)
                        return pv;
                }
            }

            return null;
        }

        public void SetVisualisationDeselected()
        {
            foreach (KeyValuePair<Feature, FeaturePolygonVisualisations> fpv in spawnedVisualisations)
            {
                List<PolygonVisualisation> visualisations = fpv.Value.Data;
                foreach (PolygonVisualisation pv in visualisations)
                {
                    if (pv != null)
                    {
                        pv.VisualisationMaterial = polygonVisualizationMaterialInstance;
                        Color col = new Color(0, 0, 0, 0);
                        List<Color> colors = new List<Color>();
                        foreach (Vector3 v in pv.PolygonMesh.vertices)
                            colors.Add(col);

                        pv.PolygonMesh.SetColors(colors);
                    }
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

            var defaultMaterial = polygonVisualizationMaterialInstance;

            // Add visualisation to the layer, and store it in the SpawnedVisualisations list where we tie our Feature
            // to the visualisations
            switch (feature.Geometry)
            {
                case MultiPolygon multiPolygon:
                    newFeatureVisualisation.AppendVisualisations(GeometryVisualizationFactory.CreatePolygonVisualization(
                        multiPolygon,
                        originalCoordinateSystem,
                        defaultMaterial
                    ));
                    break;
                case Polygon polygon:
                    newFeatureVisualisation.AppendVisualisations(GeometryVisualizationFactory.CreatePolygonVisualisation(
                        polygon,
                        originalCoordinateSystem,
                        defaultMaterial
                    ));
                    break;
            }

            // After setting up the entire visualisation - apply styling so that we use the styling system to tweak
            // this visualisation consistent with what would happen if you re-apply the styling using the ApplyStyling()
            // method
            newFeatureVisualisation.SetMaterial(polygonVisualizationMaterialInstance);

            // bounds are calculated in the AppendVisualisations method, and is therefore not explicitly called here
            spawnedVisualisations.Add(feature, newFeatureVisualisation);
            newFeatureVisualisation.ShowVisualisations(activeInHierarchy);
        }

        /// <summary>
        /// Copy the feature attributes onto the layer feature so that the styling system can
        /// use that as input to pick the correct style.
        /// </summary>
        protected LayerFeature AddAttributesToLayerFeature(LayerFeature feature)
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
            featureVisualisation.DestroyAllVisualisations();
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