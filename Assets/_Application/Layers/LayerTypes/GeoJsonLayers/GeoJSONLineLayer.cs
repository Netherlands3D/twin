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
    //TODO STYLING: add stylingpropertydata to this layer
    [Serializable]
    public partial class GeoJSONLineLayer : LayerGameObject, IGeoJsonVisualisationLayer, IVisualizationWithPropertyData
    {
        public bool IsPolygon => false;
        public override bool IsMaskable => false;

        public Transform Transform => transform;

        public event IGeoJsonVisualisationLayer.GeoJsonHandler FeatureRemoved;

        private Dictionary<Feature, FeatureLineVisualisations> spawnedVisualisations = new();
        private List<List<Coordinate>> visualisationsToRemove = new();
        private List<List<Coordinate>> selectionList = new();

        public override BoundingBox Bounds => GetBoundingBoxOfVisibleFeatures();

        [SerializeField] private LineRenderer3D lineRenderer3D;
        [SerializeField] private LineRenderer3D selectionLineRenderer3D;
        
        private GeoJsonLineLayerMaterialApplicator applicator;

        internal GeoJsonLineLayerMaterialApplicator Applicator
        {
            get
            {
                if (applicator == null) applicator = new GeoJsonLineLayerMaterialApplicator(this);

                return applicator;
            }
        }

        public LineRenderer3D LineRenderer3D
        {
            get => lineRenderer3D;
            //todo: move old lines to new renderer, remove old lines from old renderer without clearing entire list?
            set => lineRenderer3D = value;
        }

        protected override void OnLayerReady()
        {
            // Ensure that LineRenderer3D.Material has a Material Instance to prevent accidental destruction
            // of a material asset when replacing the material - no destroy of the old material must be done because
            // that is an asset and not an instance
            lineRenderer3D.LineMaterial = new Material(lineRenderer3D.LineMaterial);
            var stylingPropertyData = LayerData.GetProperty<StylingPropertyData>();
            stylingPropertyData.ActiveToolProperty = Symbolizer.StrokeColorProperty;
        }

        public List<Mesh> GetMeshData(Feature feature)
        {
            FeatureLineVisualisations data = spawnedVisualisations[feature];
            List<Mesh> meshes = new List<Mesh>();
            if (data == null)
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
        //also we are using the actual feature geometry to find the vertices in the targeted buffers
        public void SetVisualisationColor(Transform transform, List<Mesh> meshes, Color color)
        {
            selectionList.Clear();
            foreach (Mesh mesh in meshes)
            {
                Vector3[] vertices = mesh.vertices; // The meshes are from the world object, not the lineRenderer positions
                List<Coordinate> line = new List<Coordinate>();
                for (int i = 0; i < vertices.Length; i++)
                {
                    var coordinate = new Coordinate(vertices[i]);
                    line.Add(coordinate);
                }

                selectionList.Add(line);
            }

            selectionLineRenderer3D.PointMaterial.color = color;
            selectionLineRenderer3D.SetPositionCollections(selectionList);
        }

        public void SetVisualisationColorToDefault() //todo rename this?
        {
            selectionLineRenderer3D.Clear();
        }

        public Color GetRenderColor()
        {
            return LineRenderer3D.LineMaterial.color;
        }

        public override void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            LineRenderer3D.gameObject.SetActive(activeInHierarchy);
        }

        public void AddAndVisualizeFeature(Feature feature, CoordinateSystem originalCoordinateSystem)          
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

            if (feature.Geometry is LineString lineString)
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
            // The color in the Layer Panel represents the default stroke color for this layer
            StylingPropertyData stylingPropertyData = LayerData.GetProperty<StylingPropertyData>();
            LayerData.Color = stylingPropertyData.DefaultSymbolizer?.GetStrokeColor() ?? LayerData.Color;

            MaterialApplicator.Apply(this.Applicator);
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

            visualisationsToRemove.Clear();
            foreach (var kvp in spawnedVisualisations.Reverse())
            {
                var inCameraFrustum = GeometryUtility.TestPlanesAABB(frustumPlanes, kvp.Value.tiledBounds);
                if (inCameraFrustum) continue;

                visualisationsToRemove.AddRange(kvp.Value.Data);
                RemoveFeature(kvp.Value);
            }

            lineRenderer3D.RemovePointCollections(visualisationsToRemove);
        }

        private void RemoveFeature(FeatureLineVisualisations featureVisualisation)
        {
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

            var crs2D = CoordinateSystems.To2D(bbox.CoordinateSystem);
            bbox.Convert(crs2D); //remove the height, since a GeoJSON is always 2D. This is needed to make the centering work correctly
            return bbox;
        }
        
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            //copy the parent styles in this layer
            var parentStyleStyles = LayerData?.ParentLayer?.GetProperty<StylingPropertyData>().Styles;
            InitProperty<StylingPropertyData>(properties, null, parentStyleStyles);
        }
    }
}