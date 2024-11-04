using GeoJSON.Net.Feature;
using Netherlands3D.Twin.Layers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin
{
    public class FeatureMapping : MonoBehaviour
    {
        public string FeatureID { get { return feature.Id; } }
        public IGeoJsonVisualisationLayer VisualisationLayer { get { return visualisationLayer; } }
        public GeoJsonLayerGameObject VisualisationParent { get { return geoJsonLayerParent; } }
        public List<Mesh> FeatureMeshes { get { return visualisationLayer.GetMeshData(feature); } }
        public Feature Feature { get { return feature; } }

        private Feature feature;
        private List<Mesh> meshes;
        private IGeoJsonVisualisationLayer visualisationLayer;
        private GeoJsonLayerGameObject geoJsonLayerParent;
    
        public void SetGeoJsonLayerParent(GeoJsonLayerGameObject parentLayer)
        {
            geoJsonLayerParent = parentLayer;
        }

        public void SetFeature(Feature feature)
        {
            this.feature = feature; 
        }

        public void SetMeshes(List<Mesh> meshes)
        {
            this.meshes = meshes;            
        }

        public void SetVisualisationLayer(IGeoJsonVisualisationLayer visualisationLayer)
        {
            this.visualisationLayer = visualisationLayer;
        }

        public void SelectFeature()
        {
            Color selectionColor = Color.blue;
            visualisationLayer.SetVisualisationColor(meshes, selectionColor);
        }

        public void DeselectFeature()
        {
            Color renderColor = visualisationLayer.GetRenderColor();
            visualisationLayer.SetVisualisationColor(meshes, renderColor);
        }
    }
}
