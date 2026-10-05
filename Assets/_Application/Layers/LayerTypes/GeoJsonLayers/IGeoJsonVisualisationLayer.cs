using GeoJSON.Net.Feature;
using Netherlands3D.Coordinates;
using System.Collections.Generic;
using GeoJSON.Net;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers
{
    public interface IGeoJsonVisualisationLayer
    {
        bool SupportsGeometryType(GeoJSONObjectType geometryType);
        int FeatureCount { get; }
        Transform Transform { get; }
        Color RenderColor { get; set; }
        Material RenderMaterial { get; }
        List<Mesh> GetMeshData(Feature feature);
        void SetVisualisationSelected(Transform transform, List<Mesh> meshes, Color color);
        void SetVisualisationDeselected();
        void AddAndVisualizeFeature(Feature feature, CoordinateSystem originalCoordinateSystem, bool activeInHierarchy);
        Bounds GetFeatureBounds(Feature feature);
        float GetSelectionRange();
        delegate void GeoJsonHandler(Feature feature);
        event GeoJsonHandler FeatureRemoved;
    }
}
