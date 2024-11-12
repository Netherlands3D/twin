using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.UI.LayerInspector;
using Netherlands3D.Visualisers;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    [Serializable]
    public partial class GeoJSONLineLayer : LayerGameObject, IGeoJsonVisualisationLayer
    {
        public bool IsPolygon  => false;
        public Transform Transform { get => transform; }

        public List<Mesh> GetMeshData(Feature feature)
        {
            FeatureLineVisualisations data = SpawnedVisualisations.Where(f => f.feature == feature).FirstOrDefault();
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
                if (points.Count < 3)
                    continue;
                int[] triangles = new int[(points.Count - 2) * 3];
                for (int i = 0; i < points.Count - 2; i++)
                {
                    triangles[i * 3] = 0;
                    triangles[i * 3 + 1] = i + 1;
                    triangles[i * 3 + 2] = i + 2;
                }
                mesh.SetTriangles(triangles, 0);
            }
            return meshes;
        }
        
        //because the transfrom will always be at the V3zero position we dont want to offset with the localoffset
        //the vertex positions will equal world space
        public void SetVisualisationColor(Transform transform, List<Mesh> meshes, Color color)
        {
            //TODO multi lines still cause a buffer error in batchrendering, figure out if its the mesh or the batcher
            //probably the closest to point is too close to another line while looping through the transformmatrixcache
            foreach (Mesh mesh in meshes)
            {
                Vector3[] vertices = mesh.vertices;
                for (int i = 0; i < vertices.Length; i++)
                {
                    lineRenderer3D.SetLineColorClosestToPoint(transform.position + vertices[i], color);
                }
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

        public List<FeatureLineVisualisations> SpawnedVisualisations = new();

        private bool randomizeColorPerFeature = false;
        public bool RandomizeColorPerFeature { get => randomizeColorPerFeature; set => randomizeColorPerFeature = value; }

        [SerializeField] private LineRenderer3D lineRenderer3D;

        public LineRenderer3D LineRenderer3D
        {
            get { return lineRenderer3D; }
            set
            {
                //todo: move old lines to new renderer, remove old lines from old renderer without clearing entire list?
                // value.SetLines(lineRenderer3D.Lines); 
                // Destroy(lineRenderer3D.gameObject);
                lineRenderer3D = value;
            }
        }       

        public override void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            LineRenderer3D.gameObject.SetActive(activeInHierarchy);
        }

        public void AddAndVisualizeFeature<T>(Feature feature, CoordinateSystem originalCoordinateSystem)
            where T : GeoJSONObject
        {
            // Skip if feature already exists (comparison is done using hashcode based on geometry)
            if (SpawnedVisualisations.Any(f => f.feature.GetHashCode() == feature.GetHashCode()))
                return;

            var newFeatureVisualisation = new FeatureLineVisualisations() { feature = feature };

            // Create visual with random color if enabled
            lineRenderer3D.LineMaterial = GetMaterialInstance();

            if (feature.Geometry is MultiLineString multiLineString)
            {
                var newLines = GeoJSONGeometryVisualizerUtility.VisualizeMultiLineString(multiLineString, originalCoordinateSystem, lineRenderer3D);
                newFeatureVisualisation.Data.AddRange(newLines);
            }
            else if(feature.Geometry is LineString lineString)
            {
                var newLine = GeoJSONGeometryVisualizerUtility.VisualizeLineString(lineString, originalCoordinateSystem, lineRenderer3D);
                newFeatureVisualisation.Data.Add(newLine);
            }

            SpawnedVisualisations.Add(newFeatureVisualisation);
        }

        private Material GetMaterialInstance()
        {
            Material featureMaterialInstance;
            // Create material with random color if randomize per feature is enabled
            if (RandomizeColorPerFeature)
            {
                var randomColor = UnityEngine.Random.ColorHSV();
                randomColor.a = LayerData.Color.a;

                featureMaterialInstance = new Material(lineRenderer3D.LineMaterial) { color = randomColor };
                return featureMaterialInstance;
            }

            // Default to material with layer color
            featureMaterialInstance = new Material(lineRenderer3D.LineMaterial) { color = LayerData.Color };
            return featureMaterialInstance;
        }

        /// <summary>
        /// Checks the Bounds of the visualisations and checks them against the camera frustum
        /// to remove visualisations that are out of view
        /// </summary>
        public void RemoveFeaturesOutOfView()
        {         
            // Remove visualisations that are out of view
            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            for (int i = SpawnedVisualisations.Count - 1; i >= 0 ; i--)
            {
                // Make sure to recalculate bounds because they can change due to shifts
                SpawnedVisualisations[i].CalculateBounds();

                var inCameraFrustum = GeometryUtility.TestPlanesAABB(frustumPlanes, SpawnedVisualisations[i].bounds);
                if (inCameraFrustum)
                    continue;

                var featureVisualisation = SpawnedVisualisations[i];
                RemoveFeature(featureVisualisation);
            }
        }
        
        private void RemoveFeature(FeatureLineVisualisations featureVisualisation)
        {
            foreach (var line in featureVisualisation.Data)
                lineRenderer3D.RemoveLine(line);

            SpawnedVisualisations.Remove(featureVisualisation);
        }

        public override void DestroyLayerGameObject()
        {
            if (Application.isPlaying)
                GameObject.Destroy(LineRenderer3D.gameObject);

            base.DestroyLayerGameObject();
        }
    }
}