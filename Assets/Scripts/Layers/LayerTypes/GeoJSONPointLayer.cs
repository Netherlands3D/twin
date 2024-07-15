using System;
using System.Collections;
using System.Collections.Generic;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Netherlands3D.Twin
{
    [Serializable]
    public class GeoJSONPointLayer : LayerNL3DBase
    {
        public List<Feature> PointFeatures = new();

        private BatchedMeshInstanceRenderer pointRenderer3D;

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
        
        public GeoJSONPointLayer(string name) : base(name)
        {
            ProjectData.Current.AddStandardLayer(this);
        }
        
        protected override void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            pointRenderer3D.gameObject.SetActive(activeInHierarchy);
        }

        public void AddAndVisualizeFeature(Feature feature, MultiPoint featureGeometry, CoordinateSystem originalCoordinateSystem)
        {
            PointFeatures.Add(feature);
            GeoJSONGeometryVisualizerUtility.VisualizeMultiPoint(featureGeometry, originalCoordinateSystem, PointRenderer3D);
        }

        public void AddAndVisualizeFeature(Feature feature, Point featureGeometry, CoordinateSystem originalCoordinateSystem)
        {
            PointFeatures.Add(feature);
            GeoJSONGeometryVisualizerUtility.VisualizePoint(featureGeometry, originalCoordinateSystem, PointRenderer3D);
        }

        public void RemoveFeature(Feature feature)
        {
            
        }

        public override void DestroyLayer()
        {
            base.DestroyLayer();
            if (Application.isPlaying)
                GameObject.Destroy(PointRenderer3D.gameObject);
        }
    }
}