using System;
using System.Collections;
using System.Collections.Generic;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;

namespace Netherlands3D.Twin
{
    [Serializable]
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

        public GeoJSONLineLayer(string name) : base(name)
        {
            ProjectData.Current.AddStandardLayer(this);
        }
        
        protected override void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            LineRenderer3D.gameObject.SetActive(activeInHierarchy);
        }

        public void AddAndVisualizeFeature<T>(Feature feature, CoordinateSystem originalCoordinateSystem)
            where T : GeoJSONObject
        {
            LineFeatures.Add(feature);
        
            if (feature.Geometry is MultiLineString multiLineString)
            {
                GeoJSONGeometryVisualizerUtility.VisualizeMultiLineString(multiLineString, originalCoordinateSystem, lineRenderer3D);
            }
            else if(feature.Geometry is LineString lineString)
            {
                GeoJSONGeometryVisualizerUtility.VisualizeLineString(lineString, originalCoordinateSystem, lineRenderer3D);
            }
        }

        public void RemoveFeature(Feature feature)
        {
            LineFeatures.Remove(feature);
            //TODO: Remove points from renderer
        }

        public override void DestroyLayer()
        {
            base.DestroyLayer();
            if (Application.isPlaying)
                GameObject.Destroy(LineRenderer3D.gameObject);
        }

        public void RemoveFeaturesOutOfView()
        {
            
        }
    }
}