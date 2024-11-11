using System.Collections.Generic;
using GeoJSON.Net.Feature;
using Netherlands3D.Twin.Layers;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class FeatureMapping : MonoBehaviour
    {
        public string FeatureID { get { return feature.Id; } }
        public IGeoJsonVisualisationLayer VisualisationLayer { get { return visualisationLayer; } }
        public GeoJsonLayerGameObject VisualisationParent { get { return geoJsonLayerParent; } }
        public List<Mesh> FeatureMeshes { get { return visualisationLayer.GetMeshData(feature); } }
        public Feature Feature { get { return feature; } }
        public int LayerOrder { get { return geoJsonLayerParent.LayerData.RootIndex; } }

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
            visualisationLayer.SetVisualisationColor(transform, meshes, selectionColor);
        }

        public void DeselectFeature()
        {
            visualisationLayer.SetVisualisationColorToDefault();
        }
    }
}
