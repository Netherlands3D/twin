using GeoJSON.Net.Feature;
using Netherlands3D.Coordinates;
using System.Collections.Generic;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers
{
    public interface IGeoJsonVisualisationLayer
    {
        public string DisplayName { get; }
        public string StylingColorProperty { get; }
        bool SupportsGeometryType(Feature feature);
        int FeatureCount { get; }
        Transform Transform { get; }
        Color RenderColor { get; set; }
        Material RenderMaterial { get; }
        List<Mesh> GetMeshData(Feature feature);
        void SetVisualisationSelected(Transform transform, List<Mesh> meshes, Color color);
        void SetVisualisationDeselected();
        void AddAndVisualizeFeature(Feature feature, CoordinateSystem originalCoordinateSystem, bool activeInHierarchy);
        void OnLayerActiveInHierarchyChanged(bool activeInHierarchy);
        public BoundingBox GetBoundingBoxOfVisibleFeatures();
        Bounds GetFeatureBounds(Feature feature);
        float GetSelectionRange();
        void RemoveFeaturesOutOfView();
        delegate void GeoJsonHandler(Feature feature);
        event GeoJsonHandler FeatureRemoved;
    }
}
