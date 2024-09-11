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
    public partial class GeoJSONLineLayer : LayerGameObject
    {
        public List<FeatureLineVisualisations> SpawnedVisualisations = new();

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

            if (feature.Geometry is MultiLineString multiLineString)
            {
                var newLines = GeoJSONGeometryVisualizerUtility.VisualizeMultiLineString(multiLineString, originalCoordinateSystem, lineRenderer3D);
                newFeatureVisualisation.lines.AddRange(newLines);
            }
            else if(feature.Geometry is LineString lineString)
            {
                var newLine = GeoJSONGeometryVisualizerUtility.VisualizeLineString(lineString, originalCoordinateSystem, lineRenderer3D);
                newFeatureVisualisation.lines.Add(newLine);
            }

            SpawnedVisualisations.Add(newFeatureVisualisation);
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
            foreach (var line in featureVisualisation.lines)
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