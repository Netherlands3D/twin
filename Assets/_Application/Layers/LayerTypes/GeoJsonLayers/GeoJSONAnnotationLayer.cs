using System;
using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.LayerStyles;
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
            if (feature.Properties.TryGetValue("annotation", out var value) &&
                value is JObject obj)
            {
                annotation = obj.ToObject<Annotation>();
                return annotation != null;
            }

            return false;
        }

        public int FeatureCount => spawnedVisualisations.Count;

        public Transform Transform => transform;

        public event IGeoJsonVisualisationLayer.GeoJsonHandler FeatureRemoved;

        private Dictionary<Feature, GeoJSONPointLayer.FeaturePointVisualisations> spawnedVisualisations = new();
        
        private List<List<Coordinate>> visualisationsToRemove = new();

        [SerializeField] private Material annotationMaterial;

        private Annotation annotation;
        public class Annotation
        {
            public string Title { get; set; }
            public string AnnotationText { get; set; }
            public string ImageUrl { get; set; }
            public string ImageCaption { get; set; }
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

            // var newFeatureVisualisation = new GeoJSONPointLayer.FeaturePointVisualisations { feature = feature };
            //
            // if (feature.Geometry is MultiPoint multiPoint)
            // {
            //     var newPointCollection = GeometryVisualizationFactory.CreatePointVisualisation(multiPoint, originalCoordinateSystem, PointRenderer3D);
            //     newFeatureVisualisation.Data.Add(newPointCollection);
            // }
            // else if (feature.Geometry is Point point)
            // {
            //     var newPointCollection = GeometryVisualizationFactory.CreatePointVisualization(point, originalCoordinateSystem, PointRenderer3D);
            //     newFeatureVisualisation.Data.Add(newPointCollection);
            // }

            // newFeatureVisualisation.SetBoundsPadding(Vector3.one * GetSelectionRange());
            // newFeatureVisualisation.CalculateBounds();
            // spawnedVisualisations.Add(feature, newFeatureVisualisation);
        }

        /// <summary>
        /// Checks the Bounds of the visualisations and checks them against the camera frustum
        /// to remove visualisations that are out of view
        /// </summary>
        public void RemoveFeaturesOutOfView()
        {
            // var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            //
            // visualisationsToRemove.Clear();
            // foreach (var kvp in spawnedVisualisations.Reverse())
            // {
            //     var inCameraFrustum = GeometryUtility.TestPlanesAABB(frustumPlanes, kvp.Value.tiledBounds);
            //     if (inCameraFrustum) continue;
            //
            //     visualisationsToRemove.AddRange(kvp.Value.Data);
            //     RemoveFeature(kvp.Value);
            // }
            // PointRenderer3D.RemovePointCollections(visualisationsToRemove);
        }

        private void RemoveFeature(GeoJSONPointLayer.FeaturePointVisualisations featureVisualisation)
        {
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