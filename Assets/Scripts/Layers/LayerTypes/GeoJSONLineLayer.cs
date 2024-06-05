using System.Collections;
using System.Collections.Generic;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class GeoJSONLineLayer : LayerNL3DBase
    {
        public List<Feature> LineFeatures = new();

        private LineRenderer3D lineRenderer3D;
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

        protected override void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            LineRenderer3D.gameObject.SetActive(activeInHierarchy);
        }

        public void AddAndVisualizeFeature(Feature feature, MultiLineString featureGeometry, CoordinateSystem originalCoordinateSystem)
        {
            LineFeatures.Add(feature);
            GeoJSONGeometryVisualizerUtility.VisualizeMultiLineString(featureGeometry, originalCoordinateSystem, LineRenderer3D);
        }

        public void AddAndVisualizeFeature(Feature feature, LineString featureGeometry, CoordinateSystem originalCoordinateSystem)
        {
            LineFeatures.Add(feature);
            GeoJSONGeometryVisualizerUtility.VisualizeLineString(featureGeometry, originalCoordinateSystem, LineRenderer3D);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Destroy(LineRenderer3D.gameObject);
        }
    }
}