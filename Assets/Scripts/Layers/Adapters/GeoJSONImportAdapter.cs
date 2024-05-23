using System.IO;
using UnityEngine;

namespace Netherlands3D.Twin
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/GeoJSONImportAdapter", fileName = "GeoJSONImportAdapter", order = 0)]
    public class GeoJSONImportAdapter : ScriptableObject
    {
        [SerializeField] private Material visualizationMaterial;
        public void ParseGeoJSON(string file)
        {
            var fullPath = Path.Combine(Application.persistentDataPath, file);
            CreateGeoJSONLayer(fullPath, visualizationMaterial);
        }

        public static GeoJSONLayer CreateGeoJSONLayer(string filePath, Material visualizationMaterial)
        {
            var go = new GameObject("GeoJSON");
            var layer = go.AddComponent<GeoJSONLayer>();
            layer.VisualizationMaterial = visualizationMaterial;
            layer.ParseGeoJSON(filePath);
            return layer;
        }
    }
}