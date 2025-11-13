using GeoJSON.Net.Feature;
using Netherlands3D.Coordinates;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers
{
    public interface IGeoJsonVisualisationLayer : ILayerWithPropertyPanels
    {
        LayerData LayerData { get; }
        bool IsPolygon { get; }
        Transform Transform { get; }
        Color GetRenderColor();
        List<Mesh> GetMeshData(Feature feature);
        void SetVisualisationColor(Transform transform, List<Mesh> meshes, Color color);
        void SetVisualisationColorToDefault();
        void AddAndVisualizeFeature(Feature feature, CoordinateSystem originalCoordinateSystem);
        Bounds GetFeatureBounds(Feature feature);
        float GetSelectionRange();

        delegate void GeoJsonHandler(Feature feature);
        event GeoJsonHandler FeatureRemoved;
    }
}
