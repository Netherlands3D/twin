
using GeoJSON.Net.Feature;
using Netherlands3D.Coordinates;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public interface IGeoJsonVisualisationLayer
    {
        bool IsPolygon { get; }
        Transform Transform { get; }
        Color GetRenderColor();
        List<Mesh> GetMeshData(Feature feature);
        void SetVisualisationColor(Transform transform, List<Mesh> meshes, Color color);
        void SetVisualisationColorToDefault();
    }
}
