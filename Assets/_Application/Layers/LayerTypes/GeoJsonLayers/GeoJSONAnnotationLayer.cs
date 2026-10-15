using System;
using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Utility;
using Netherlands3D.UI.Components;
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

        private Dictionary<Feature, FeatureAnnotationVisualisations> spawnedVisualisations = new();

        [SerializeField] private Material annotationMaterial;

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

        public Material RenderMaterial => annotationMaterial;

        public List<Mesh> GetMeshData(Feature feature) => null;

        public Bounds GetFeatureBounds(Feature feature)
        {
            return spawnedVisualisations[feature].trueBounds;
        }

        public float GetSelectionRange()
        {
            return WorldAnnotation.Size;
        }

        //here we have to local offset the vertices with the position of the transform because the transform gets shifted
        //also we are using the actual feature geometry to find the vertices in the targeted buffers
        public void SetVisualisationSelected(Transform transform, List<Mesh> meshes, Color color)
        {
           
        }

        public void SetVisualisationDeselected() 
        {
            
        }

        private bool activeInHierarchy;
        public void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            this.activeInHierarchy = activeInHierarchy;
            foreach (var kvp in spawnedVisualisations)
            {
                kvp.Value.SetVisible(activeInHierarchy);
            }
        }

        public void AddAndVisualizeFeature(Feature feature, CoordinateSystem originalCoordinateSystem, bool activeInHierarchy)
        {
            // Skip if feature already exists (comparison is done using hashcode based on geometry)
            if (spawnedVisualisations.ContainsKey(feature))
                return;
            
            if (feature.Properties.TryGetValue("annotation", out var value) && value is JObject obj)
            {
                FeatureAnnotationVisualisations.Annotation annotation = obj.ToObject<FeatureAnnotationVisualisations.Annotation>();
                FeatureAnnotationVisualisations visualisation = new FeatureAnnotationVisualisations(annotation) { Feature = feature };
                
                Point point = feature.Geometry as Point;
                var convertedPoint = GeometryVisualizationFactory.ConvertToCoordinate(originalCoordinateSystem, point.Coordinates);
                var singlePointList = new List<Coordinate>() { convertedPoint };
                visualisation.Data.Add(singlePointList);
                
                visualisation.SetBoundsPadding(Vector3.one * GetSelectionRange());
                visualisation.CalculateBounds();
                spawnedVisualisations.Add(feature, visualisation);
            }
        }

        private void Update()
        {
            if(!activeInHierarchy) return;
            
            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            foreach (var kvp in spawnedVisualisations)
            {
                var inCameraFrustum = GeometryUtility.TestPlanesAABB(frustumPlanes, kvp.Value.tiledBounds);
                kvp.Value.SetVisible(inCameraFrustum);
                
                if (!inCameraFrustum) continue;

                kvp.Value.Update();
            }
        }
    
        public void RemoveFeaturesOutOfView()
        {
            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            foreach (var kvp in spawnedVisualisations.Reverse())
            {
                var inCameraFrustum = GeometryUtility.TestPlanesAABB(frustumPlanes, kvp.Value.tiledBounds);
                if (inCameraFrustum) continue;
                
                RemoveFeature(kvp.Key);
            }
        }
        
        private void RemoveFeature(Feature feature)
        {
            FeatureRemoved?.Invoke(feature);
            spawnedVisualisations[feature].Dispose();
            spawnedVisualisations.Remove(feature);
        }
        
        void OnDestroy()
        {
            foreach (var kvp in spawnedVisualisations.Reverse())
            {
                RemoveFeature(kvp.Key);
            }
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