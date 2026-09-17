using GeoJSON.Net.Feature;
using Netherlands3D.Coordinates;
using System.Collections.Generic;
using GeoJSON.Net;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers
{
    public interface IGeoJsonVisualisationLayer
    {
        bool SupportsGeometryType(Feature feature);
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

    public class GeoJsonAnnotationVisualisationLayer : IGeoJsonVisualisationLayer
    {
        public bool SupportsGeometryType(Feature geometryType)
        {
            return geometryType.Equals(GeoJSONObjectType.Point) && geometryType.Properties.ContainsKey("imageUrl");
        }

        public int FeatureCount { get; }
        public Transform Transform { get; }
        public Color RenderColor { get; set; }
        public Material RenderMaterial { get; }
        public List<Mesh> GetMeshData(Feature feature)
        {
            throw new System.NotImplementedException();
        }

        public void SetVisualisationSelected(Transform transform, List<Mesh> meshes, Color color)
        {
            throw new System.NotImplementedException();
        }

        public void SetVisualisationDeselected()
        {
            throw new System.NotImplementedException();
        }

        public void AddAndVisualizeFeature(Feature feature, CoordinateSystem originalCoordinateSystem, bool activeInHierarchy)
        {
            throw new System.NotImplementedException();
        }

        public Bounds GetFeatureBounds(Feature feature)
        {
            throw new System.NotImplementedException();
        }

        public float GetSelectionRange()
        {
            throw new System.NotImplementedException();
        }

        public event IGeoJsonVisualisationLayer.GeoJsonHandler FeatureRemoved;
    }
}
