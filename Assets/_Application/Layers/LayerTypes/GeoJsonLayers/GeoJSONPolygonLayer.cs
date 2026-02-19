using System;
using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers
{
    [Serializable]
    public partial class GeoJSONPolygonLayer : LayerGameObject, IGeoJsonVisualisationLayer, IVisualizationWithPropertyData
    {
        private GeoJsonPolygonLayerMaterialApplicator applicator;
        internal GeoJsonPolygonLayerMaterialApplicator Applicator
        {
            get
            {
                if (applicator == null) applicator = new GeoJsonPolygonLayerMaterialApplicator(this);

                return applicator;
            }
        }
        
        public override BoundingBox Bounds => GetBoundingBoxOfVisibleFeatures();
        public bool IsPolygon => true;
        public Transform Transform { get => transform; }
        public event IGeoJsonVisualisationLayer.GeoJsonHandler FeatureRemoved;

        private Dictionary<Feature, FeaturePolygonVisualisations> spawnedVisualisations = new();     
        
        [SerializeField] private Material polygonVisualizationMaterial;
        
        internal Material polygonVisualizationMaterialInstance;
        [SerializeField] private Material polygonSelectionVisualizationMaterial;

        public Material PolygonVisualizationMaterial
        {
            get => polygonVisualizationMaterial;
            set
            {
                // This counts as a shared material - as such we create a copy of the material and assign that
                polygonVisualizationMaterial = value;
                if (polygonVisualizationMaterial == null)
                {
                    polygonVisualizationMaterialInstance = null;
                    return;
                }
                polygonVisualizationMaterialInstance = new Material(value);

                ApplyStyling();
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
                if(polygon.PolygonMesh == null)
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
        public void SetVisualisationColor(Transform transform, List<Mesh> meshes, Color color)
        {
            foreach (var mesh in meshes)
            {
                PolygonVisualisation visualisation = GetPolygonVisualisationByMesh(mesh);
                if (visualisation != null)
                {
                    visualisation.VisualisationMaterial = polygonSelectionVisualizationMaterial;
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

        public void SetVisualisationColorToDefault()
        {
            foreach (KeyValuePair<Feature, FeaturePolygonVisualisations> fpv in spawnedVisualisations)
            {
                List<PolygonVisualisation> visualisations = fpv.Value.Data;
                foreach (PolygonVisualisation pv in visualisations)
                {
                    if (pv != null)
                    {
                        pv.VisualisationMaterial = polygonVisualizationMaterialInstance;
                    }
                }
            }
        }

        public Color GetRenderColor()
        {
            return polygonVisualizationMaterialInstance.color;
        }
     
        public override void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            foreach (var visualization in spawnedVisualisations)
            {
                visualization.Value.ShowVisualisations(activeInHierarchy);
            }
        }

        public void AddAndVisualizeFeature(Feature feature, CoordinateSystem originalCoordinateSystem)           
        {
            // Skip if feature already exists (comparison is done using hashcode based on geometry)
            if (spawnedVisualisations.ContainsKey(feature))
                return;

            var newFeatureVisualisation = new FeaturePolygonVisualisations { 
                feature = feature,
                geoJsonPolygonLayer = this
            };

            var defaultMaterial = polygonVisualizationMaterialInstance ?? GetMaterialInstance(Color.white);
            
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
            ApplyStyling(newFeatureVisualisation);

            // bounds are calculated in the AppendVisualisations method, and is therefore not explicitly called here
            spawnedVisualisations.Add(feature, newFeatureVisualisation);
            newFeatureVisualisation.ShowVisualisations(LayerData.ActiveInHierarchy);
        }

        public override void ApplyStyling()
        {
            MaterialApplicator.Apply(Applicator);
            foreach (var visualisation in spawnedVisualisations)
            {
                ApplyStyling(visualisation.Value);
            }
            // The color in the Layer Panel represents the default fill color for this layer
            LayerData.Color = Applicator.GetMaterial().color;
        }

        public void ApplyStyling(FeaturePolygonVisualisations visualisation)
        {
            visualisation.SetMaterial(polygonVisualizationMaterialInstance);
        }

        /// <summary>
        /// Copy the feature attributes onto the layer feature so that the styling system can
        /// use that as input to pick the correct style.
        /// </summary>
        protected override LayerFeature AddAttributesToLayerFeature(LayerFeature feature)
        {
            // it should be a FeaturePolygonVisualisations, just do a sanity check here
            if (feature.Geometry is not FeaturePolygonVisualisations visualisations) return feature;

            foreach (var property in visualisations.feature.Properties)
            {
                feature.Attributes.Add(property.Key, property.Value.ToString());
            }
            
            return feature;
        }

        private Material GetMaterialInstance(Color color)
        {
            if (
                !polygonVisualizationMaterialInstance 
                || polygonVisualizationMaterialInstance.color != color
            ) {
                polygonVisualizationMaterialInstance = new Material(PolygonVisualizationMaterial)
                {
                    color = color
                };
            }

            return polygonVisualizationMaterialInstance;
        }

        public override void DestroyLayerGameObject()
        {
            // Remove all SpawnedVisualisations
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

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            InitProperty<ColorPropertyData>(properties); 
        }
    }
}