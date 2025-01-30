using System.Collections.Generic;
using GeoJSON.Net.Feature;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using UnityEngine;

namespace Netherlands3D.Functionalities.ObjectInformation
{
    public class FeatureMapping : MonoBehaviour
    {
        public string FeatureID => feature.Id;
        public IGeoJsonVisualisationLayer VisualisationLayer { get { return visualisationLayer; } }
        public GeoJsonLayerGameObject VisualisationParent { get { return geoJsonLayerParent; } }
        public List<Mesh> FeatureMeshes { get { return visualisationLayer.GetMeshData(feature); } }
        public Feature Feature { get { return feature; } }
        public int LayerOrder { get { return geoJsonLayerParent.LayerData.RootIndex; } }
        public Coordinate Position;

        private Feature feature;
        private List<Mesh> meshes;
        private IGeoJsonVisualisationLayer visualisationLayer;
        private GeoJsonLayerGameObject geoJsonLayerParent;

        public void SetGeoJsonLayerParent(GeoJsonLayerGameObject parentLayer)
        {
            geoJsonLayerParent = parentLayer;
        }

        public void SetPosition(Coordinate position)
        {
            this.Position = position;
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
            foreach (Mesh mesh in meshes)
            {
                Color[] colors = new Color[mesh.vertexCount];
                for (int i = 0; i < mesh.vertexCount; i++)
                    colors[i] = THUMBNAIL_COLOR;
                mesh.SetColors(colors);
            }
        }

        public void DeselectFeature()
        {
            visualisationLayer.SetVisualisationColorToDefault();
            foreach (Mesh mesh in meshes)
            {
                Color[] colors = new Color[mesh.vertexCount];
                for (int i = 0; i < mesh.vertexCount; i++)
                    colors[i] = NO_OVERRIDE_COLOR;
                mesh.SetColors(colors);
            }
        }

        private static readonly Color NO_OVERRIDE_COLOR = new Color(0, 0, 1, 0);
        private static readonly Color THUMBNAIL_COLOR = new Color(1, 0, 0, 0);  


    }
}
