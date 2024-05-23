using System.IO;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/GeoJSONImportAdapter", fileName = "GeoJSONImportAdapter", order = 0)]
    public class GeoJSONImportAdapter : ScriptableObject
    {
        [SerializeField] private Material visualizationMaterial;
        [SerializeField] private UnityEvent<string> displayErrorMessageEvent;

        public void ParseGeoJSON(string file)
        {
            var fullPath = Path.Combine(Application.persistentDataPath, file);

            var randomColorVisualisationMaterial = new Material(visualizationMaterial);
            randomColorVisualisationMaterial.color = Color.HSVToRGB(Random.value, Random.Range(0.5f, 1f), 1);
            CreateGeoJSONLayer(fullPath, randomColorVisualisationMaterial, displayErrorMessageEvent);
        }

        public static GeoJSONLayer CreateGeoJSONLayer(string filePath, Material visualizationMaterial, UnityEvent<string> onErrorCallback = null)
        {
            var go = new GameObject("GeoJSON");
            var layer = go.AddComponent<GeoJSONLayer>();

            if (onErrorCallback != null)
                layer.OnParseError.AddListener(onErrorCallback.Invoke);
            
            layer.VisualizationMaterial = visualizationMaterial;
            layer.ParseGeoJSON(filePath);
            return layer;
        }
    }
}