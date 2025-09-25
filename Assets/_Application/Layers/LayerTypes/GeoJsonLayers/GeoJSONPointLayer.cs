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
using RSG;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers
{
    [Serializable]
    public partial class GeoJSONPointLayer : LayerGameObject, IGeoJsonVisualisationLayer
    {
        [SerializeField] private BatchedMeshInstanceRenderer pointRenderer3D;
        public bool IsPolygon => false;
        public override bool IsMaskable => false;

        public Transform Transform => transform;
        public delegate void GeoJSONPointHandler(Feature feature);
        public event GeoJSONPointHandler FeatureRemoved;

        private Dictionary<Feature, FeaturePointVisualisations> spawnedVisualisations = new();
        public override BoundingBox Bounds => GetBoundingBoxOfVisibleFeatures();

        private GeoJsonPointLayerMaterialApplicator applicator;
        internal GeoJsonPointLayerMaterialApplicator Applicator
        {
            get
            {
                if (applicator == null) applicator = new GeoJsonPointLayerMaterialApplicator(this);

                return applicator;
            }
        }

        protected override void OnLayerReady()
        {
            // Ensure that PointRenderer3D.Material has a Material Instance to prevent accidental destruction
            // of a material asset when replacing the material - no destroy of the old material must be done because
            // that is an asset and not an instance
            PointRenderer3D.Material = new Material(PointRenderer3D.Material);
        }

        public List<Mesh> GetMeshData(Feature feature)
        {
            FeaturePointVisualisations data = spawnedVisualisations[feature];
            List<Mesh> meshes = new List<Mesh>();
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
            return pointRenderer3D.MeshScale;
        }


        //here we have to local offset the vertices with the position of the transform because the transform gets shifted
        public void SetVisualisationColor(Transform transform, List<Mesh> meshes, Color color)
        {
            foreach (Mesh mesh in meshes)
            {
                Vector3[] vertices = mesh.vertices;
                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector3 localOffset = vertices[i] - mesh.bounds.center;
                    pointRenderer3D.SetLineColorClosestToPoint(transform.position + localOffset, color);
                }
            }
        }

        public void SetVisualisationColorToDefault()
        {
            PointRenderer3D.SetDefaultColors();
        }

        public Color GetRenderColor()
        {
            return pointRenderer3D.Material.color;
        }
        
        public BatchedMeshInstanceRenderer PointRenderer3D
        {
            get { return pointRenderer3D; }
            set
            {
                //todo: move old lines to new renderer, remove old lines from old renderer without clearing entire list?
                // value.SetPositionCollections(pointRenderer3D.PositionCollections); 
                // Destroy(pointRenderer3D.gameObject);
                pointRenderer3D = value;
            }
        }

        public override void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            pointRenderer3D.gameObject.SetActive(activeInHierarchy);
        }

        public void AddAndVisualizeFeature<T>(Feature feature, CoordinateSystem originalCoordinateSystem)
            where T : GeoJSONObject
        {
            // Skip if feature already exists (comparison is done using hashcode based on geometry)
            if (spawnedVisualisations.ContainsKey(feature))
                return;

            var newFeatureVisualisation = new FeaturePointVisualisations { feature = feature };
            ApplyStyling(newFeatureVisualisation);

            if (feature.Geometry is MultiPoint multiPoint)
            {
                var newPointCollection = GeometryVisualizationFactory.CreatePointVisualisation(multiPoint, originalCoordinateSystem, PointRenderer3D);
                newFeatureVisualisation.Data.Add(newPointCollection);
            }
            else if (feature.Geometry is Point point)
            {
                var newPointCollection = GeometryVisualizationFactory.CreatePointVisualization(point, originalCoordinateSystem, PointRenderer3D);
                newFeatureVisualisation.Data.Add(newPointCollection);
            }

            newFeatureVisualisation.SetBoundsPadding(Vector3.one * GetSelectionRange());
            newFeatureVisualisation.CalculateBounds();
            spawnedVisualisations.Add(feature, newFeatureVisualisation);
        }

        public override void ApplyStyling()
        {
            // The color in the Layer Panel represents the default fill color for this layer
            LayerData.Color = LayerData.DefaultSymbolizer?.GetFillColor() ?? LayerData.Color;

            MaterialApplicator.Apply(Applicator);
        }

        public void ApplyStyling(FeaturePointVisualisations newFeatureVisualisation)
        {
            // Currently we don't apply individual styling per feature
        }

        private Material GetMaterialInstance(Color color)
        {
            return new Material(pointRenderer3D.Material)
            {
                color = color
            };
        }

        /// <summary>
        /// Checks the Bounds of the visualisations and checks them against the camera frustum
        /// to remove visualisations that are out of view
        /// </summary>
        public void RemoveFeaturesOutOfView()
        {
            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            foreach (var kvp in spawnedVisualisations.Reverse())
            {
                var inCameraFrustum = GeometryUtility.TestPlanesAABB(frustumPlanes, kvp.Value.tiledBounds);
                if (inCameraFrustum) continue;

                RemoveFeature(kvp.Value);
            }            
        }

        private void RemoveFeature(FeaturePointVisualisations featureVisualisation)
        {
            foreach (var pointCollection in featureVisualisation.Data)
            {
                PointRenderer3D.RemoveCollection(pointCollection);
            }
            FeatureRemoved?.Invoke(featureVisualisation.feature); 
            spawnedVisualisations.Remove(featureVisualisation.feature);
        }
        
        public override void DestroyLayerGameObject()
        {
            if (Application.isPlaying && PointRenderer3D?.gameObject)
                GameObject.Destroy(PointRenderer3D.gameObject);

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