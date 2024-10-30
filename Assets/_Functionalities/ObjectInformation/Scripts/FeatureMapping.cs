using GeoJSON.Net.Feature;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin
{
    public class FeatureMapping : MonoBehaviour
    {
        private Feature feature;
        private List<Mesh> meshes;
        private IGeoJsonVisualisationLayer visualisationLayer;
        private Color[] vertexColors;

        public void SetFeature(Feature feature)
        {
            this.feature = feature; 
        }

        public void SetMeshes(List<Mesh> meshes)
        {
            this.meshes = meshes;
            int totalVertexCount = 0;
            foreach (Mesh m in meshes)
                totalVertexCount += m.vertexCount;
            vertexColors = new Color[totalVertexCount];
        }

        public void SetVisualisationLayer(IGeoJsonVisualisationLayer visualisationLayer)
        {
            this.visualisationLayer = visualisationLayer;
        }

        public void SelectFeature()
        {
            for (int i = 0; i < vertexColors.Length; i++)
                vertexColors[i] = Color.blue;

            visualisationLayer.SetVisualisationColor(meshes, vertexColors);
        }

        public void DeselectFeature()
        {
            //todo get instance material formn the visualisation layer to set back colors and remove the previouscolors functions, its bad and prone to bugs :D
            visualisationLayer.SetVisualisationColor(meshes, vertexColors);
        }
    }
}
