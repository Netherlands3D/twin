
using GeoJSON.Net.Feature;
using Netherlands3D.Coordinates;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public interface IGeoJsonVisualisationLayer
    {
        List<Mesh> GetMeshData(Feature feature);
        void SetVisualisationColor(List<Mesh> meshes, Color[] previousColors);
    }
}
