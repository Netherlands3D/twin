using System;
using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Rendering;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers
{
    [Serializable]
    public partial class GeoJSONLineLayer : LayerGameObject, IGeoJsonVisualisationLayer
    {
        public bool IsPolygon  => false;
        public Transform Transform => transform;
        public delegate void GeoJSONLineHandler(Feature feature);
        public event GeoJSONLineHandler FeatureRemoved;

        private Dictionary<Feature, FeatureLineVisualisations> spawnedVisualisations = new();

        public override BoundingBox Bounds => GetBoundingBoxOfVisibleFeatures();

        [SerializeField] private LineRenderer3D lineRenderer3D;

        public LineRenderer3D LineRenderer3D
        {
            get => lineRenderer3D;
            //todo: move old lines to new renderer, remove old lines from old renderer without clearing entire list?
            set => lineRenderer3D = value;
        }

        public List<Mesh> GetMeshData(Feature feature)
        {
            FeatureLineVisualisations data = spawnedVisualisations[feature];
            List<Mesh> meshes = new List<Mesh>();
            if(data == null)
            {
                Debug.LogWarning("visualisation was not spawned for feature" + feature.Id);
                return meshes;
            }

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

        public Bounds GetFeatureBounds(Feature feature)
        {
            return spawnedVisualisations[feature].trueBounds;
        }

        public float GetSelectionRange()
        {
            return lineRenderer3D.LineDiameter;
        }

        //because the transfrom will always be at the V3zero position we dont want to offset with the localoffset
        //the vertex positions will equal world space
        public void SetVisualisationColor(Transform transform, List<Mesh> meshes, Color color)
        {
            lineRenderer3D.SetDefaultColors();
            foreach (Mesh mesh in meshes)
            {              
                Vector3[] vertices = mesh.vertices;                
                lineRenderer3D.SetLineColorFromPoints(vertices, color);
            }
        }

        public void SetVisualisationColorToDefault()
        {
            lineRenderer3D.SetDefaultColors();
        }

        public Color GetRenderColor()
        {
            return LineRenderer3D.LineMaterial.color;
        }

        public override void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            LineRenderer3D.gameObject.SetActive(activeInHierarchy);
        }

        public void AddAndVisualizeFeature<T>(Feature feature, CoordinateSystem originalCoordinateSystem)
            where T : GeoJSONObject
        {
            // Skip if feature already exists (comparison is done using hashcode based on geometry)
            if (spawnedVisualisations.ContainsKey(feature)) return;

            var newFeatureVisualisation = new FeatureLineVisualisations { feature = feature };

            ApplyStyling(newFeatureVisualisation);

            if (feature.Geometry is MultiLineString multiLineString)
            {
                var newLines = GeometryVisualizationFactory.CreateLineVisualisation(multiLineString, originalCoordinateSystem, lineRenderer3D);
                newFeatureVisualisation.Data.AddRange(newLines);
            }
            else if(feature.Geometry is LineString lineString)
            {
                var newLine = GeometryVisualizationFactory.CreateLineVisualization(lineString, originalCoordinateSystem, lineRenderer3D);
                newFeatureVisualisation.Data.Add(newLine);
            }
            
            newFeatureVisualisation.SetBoundsPadding(Vector3.one * GetSelectionRange());
            newFeatureVisualisation.CalculateBounds();
            
            spawnedVisualisations.Add(feature, newFeatureVisualisation);
        }

        public override void ApplyStyling()
        {
            // The color in the Layer Panel represents the default fill color for this layer
            LayerData.Color = LayerData.DefaultSymbolizer?.GetFillColor() ?? LayerData.Color;

            // TODO: We implement per-feature styling in a separate story; this means that for styling purposes
            //   we consider this whole layer to be a single feature at the moment
            var features = GetFeatures<BatchedMeshInstanceRenderer>();
            var style = GetStyling(features.FirstOrDefault());
            var color = style.GetFillColor() ?? Color.white;
            
            lineRenderer3D.LineMaterial = GetMaterialInstance(color);
        }

        public void ApplyStyling(FeatureLineVisualisations newFeatureVisualisation)
        {
            // Currently we don't apply individual styling per feature
        }
        
        private Material GetMaterialInstance(Color strokeColor)
        {
            return new Material(lineRenderer3D.LineMaterial)
            {
                color = strokeColor
            };
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
                if (inCameraFrustum) continue;

                RemoveFeature(kvp.Value);
            }
        }
        
        private void RemoveFeature(FeatureLineVisualisations featureVisualisation)
        {
            foreach (var line in featureVisualisation.Data)
            {
                lineRenderer3D.RemoveLine(line);
            }            
            FeatureRemoved?.Invoke(featureVisualisation.feature); 
            spawnedVisualisations.Remove(featureVisualisation.feature);
        }

        public override void DestroyLayerGameObject()
        {
            if (Application.isPlaying)
            {
                Destroy(LineRenderer3D.gameObject);
            }

            base.DestroyLayerGameObject();
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

            return bbox;
        }
        
        private List<IPropertySectionInstantiator> propertySections;

        protected List<IPropertySectionInstantiator> PropertySections
        {
            get
            {
                if (propertySections == null)
                {
                    propertySections = GetComponents<IPropertySectionInstantiator>().ToList();
                }

                return propertySections;
            }
            set => propertySections = value;
        }

        public List<IPropertySectionInstantiator> GetPropertySections()
        {
            return PropertySections;
        }
    }
}