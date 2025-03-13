using GeoJSON.Net.Feature;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers
{
    public interface IGeoJsonVisualisationLayer : ILayerWithPropertyPanels
    {
        bool IsPolygon { get; }
        Transform Transform { get; }
        Color GetRenderColor();
        List<Mesh> GetMeshData(Feature feature);
        void SetVisualisationColor(Transform transform, List<Mesh> meshes, Color color);
        void SetVisualisationColorToDefault();
        Bounds GetFeatureBounds(Feature feature);
        float GetSelectionRange();
    }
}
