using System;
using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Rendering;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers
{
    [Serializable]
    public partial class GeoJSONLineLayer : LayerGameObject, IGeoJsonVisualisationLayer
    {
        public bool IsPolygon  => false;
        public Transform Transform => transform;

        public Dictionary<Feature, FeatureLineVisualisations> SpawnedVisualisations = new();

        [SerializeField] private LineRenderer3D lineRenderer3D;

        public LineRenderer3D LineRenderer3D
        {
            get => lineRenderer3D;
            //todo: move old lines to new renderer, remove old lines from old renderer without clearing entire list?
            set => lineRenderer3D = value;
        }

        public List<Mesh> GetMeshData(Feature feature)
        {
            FeatureLineVisualisations data = SpawnedVisualisations[feature];
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
            if (SpawnedVisualisations.ContainsKey(feature)) return;

            var newFeatureVisualisation = new FeatureLineVisualisations { feature = feature };

            ApplyStyling();

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
            
            newFeatureVisualisation.CalculateBounds();
            SpawnedVisualisations.Add(feature, newFeatureVisualisation);
        }

        public override void InitializeStyling()
        {
            lineRenderer3D.LineMaterial = GetMaterialInstance();
        }

        public void ApplyStyling()
        {
            // Currently we don't apply individual styling per feature
        }
        
        private Material GetMaterialInstance()
        {
            var strokeColor = LayerData.DefaultSymbolizer.GetStrokeColor() ?? Color.white;
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
            foreach (var kvp in SpawnedVisualisations.Reverse())
            {
                var inCameraFrustum = GeometryUtility.TestPlanesAABB(frustumPlanes, kvp.Value.bounds);
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

            SpawnedVisualisations.Remove(featureVisualisation.feature);
        }

        public override void DestroyLayerGameObject()
        {
            if (Application.isPlaying)
            {
                Destroy(LineRenderer3D.gameObject);
            }

            base.DestroyLayerGameObject();
        }
    }
}