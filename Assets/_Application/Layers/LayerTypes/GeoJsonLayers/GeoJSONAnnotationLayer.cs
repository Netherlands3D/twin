using System;
using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Rendering;
using Netherlands3D.Twin.Utility;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers
{
    [Serializable]
    public class GeoJSONAnnotationLayer : MonoBehaviour, IGeoJsonVisualisationLayer
    {
        public string DisplayName => "Annotaties";
        public string StylingColorProperty => Symbolizer.FillColorProperty;
        public bool SupportsGeometryType(Feature feature)
        {
            return feature.Properties.ContainsKey("annotation");
        }

        public int FeatureCount => spawnedVisualisations.Count;

        public Transform Transform => transform;

        public event IGeoJsonVisualisationLayer.GeoJsonHandler FeatureRemoved;

        private Dictionary<Feature, Annotation> spawnedVisualisations = new();

        [SerializeField] private Material annotationMaterial;

        private Annotation annotation;

        public class Annotation
        {
            public string Title { get; set; }
            public string AnnotationText { get; set; }
            public string ImageUrl { get; set; }
            public string ImageCaption { get; set; }
            public Coordinate Coordinate { get; set; }
            public Bounds trueBounds { get; set; }
            public Bounds tiledBounds { get; set; }

            public Feature Feature { get; set; }
            public CoordinateSystem CoordinateSystem { get; set; }

            public void CalculateBounds(Coordinate coord, Coordinate coord2)
            {
                Point point = Feature.Geometry as Point;
                var convertedPoint =
                    GeometryVisualizationFactory.ConvertToCoordinate(CoordinateSystem, point.Coordinates);
                Coordinate = convertedPoint;

                trueBounds = new Bounds(convertedPoint.ToUnity(), Vector3.zero);
                trueBounds.Expand(Vector3.one * 5);
                Bounds tiledBounds = new Bounds(trueBounds.center, trueBounds.size);
                // Expand bounds to ceiling to steps
                float BoundsRoundingCeiling = 1000;
                tiledBounds.size = new Vector3(
                    Mathf.Ceil(tiledBounds.size.x / BoundsRoundingCeiling) * BoundsRoundingCeiling,
                    Mathf.Ceil(tiledBounds.size.y / BoundsRoundingCeiling) * BoundsRoundingCeiling,
                    Mathf.Ceil(tiledBounds.size.z / BoundsRoundingCeiling) * BoundsRoundingCeiling
                );
                tiledBounds.center = new Vector3(
                    Mathf.Round(tiledBounds.center.x / BoundsRoundingCeiling) * BoundsRoundingCeiling,
                    Mathf.Round(tiledBounds.center.y / BoundsRoundingCeiling) * BoundsRoundingCeiling,
                    Mathf.Round(tiledBounds.center.z / BoundsRoundingCeiling) * BoundsRoundingCeiling
                );
                this.tiledBounds = tiledBounds;
            }
        }


        public Color RenderColor
        {
            get
            {
                return annotationMaterial.color;
            }
            set
            {
                annotationMaterial = new Material(annotationMaterial);
            }
        }

        public Material RenderMaterial
        {
            get
            {
                return annotationMaterial;
            }
        }

        public List<Mesh> GetMeshData(Feature feature)
        {
            return null;
        }

        public Bounds GetFeatureBounds(Feature feature)
        {
            return spawnedVisualisations[feature].trueBounds;
        }

        public float GetSelectionRange()
        {
            return 5;
        }

        //here we have to local offset the vertices with the position of the transform because the transform gets shifted
        //also we are using the actual feature geometry to find the vertices in the targeted buffers
        public void SetVisualisationSelected(Transform transform, List<Mesh> meshes, Color color)
        {
           
        }

        public void SetVisualisationDeselected() 
        {
            
        }

        public void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            
        }

        public void AddAndVisualizeFeature(Feature feature, CoordinateSystem originalCoordinateSystem, bool activeInHierarchy)
        {
            // Skip if feature already exists (comparison is done using hashcode based on geometry)
            if (spawnedVisualisations.ContainsKey(feature))
                return;

            GeoJSONAnnotationLayer.Annotation annotation = null;
            if (feature.Properties.TryGetValue("annotation", out var value) &&
                value is JObject obj)
            {
                annotation = obj.ToObject<GeoJSONAnnotationLayer.Annotation>();
                Origin.current.onPostShift.AddListener(annotation.CalculateBounds);
            }

            
            spawnedVisualisations.Add(feature, annotation);
            
            testObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            testObject.transform.localScale = Vector3.one * GetSelectionRange();
            testObject.transform.position = annotation.trueBounds.center;
        }

        private GameObject testObject = null;
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
                
                Origin.current.onPostShift.RemoveListener(kvp.Value.CalculateBounds);
                RemoveFeature(kvp.Key);
            }
        }

        private void RemoveFeature(Feature feature)
        {
            FeatureRemoved?.Invoke(feature);
            spawnedVisualisations.Remove(feature);
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