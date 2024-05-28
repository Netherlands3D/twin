using System.IO;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/GeoJSONImportAdapter", fileName = "GeoJSONImportAdapter", order = 0)]
    public class GeoJSONImportAdapter : ScriptableObject
    {
        [SerializeField] private Material visualizationMaterial;
        [SerializeField] private LineRenderer3D lineRenderer3D;
        [SerializeField] private UnityEvent<string> displayErrorMessageEvent;

        public void ParseGeoJSON(string file)
        {
            var fullPath = Path.Combine(Application.persistentDataPath, file);

            var randomColorVisualisationMaterial = new Material(visualizationMaterial);
            randomColorVisualisationMaterial.color = Color.HSVToRGB(Random.value, Random.Range(0.5f, 1f), 1);
            CreateGeoJSONLayer(fullPath, randomColorVisualisationMaterial, lineRenderer3D, displayErrorMessageEvent);
        }

        public static GeoJSONLayer CreateGeoJSONLayer(string filePath, Material visualizationMaterial, LineRenderer3D lineRenderer3D, UnityEvent<string> onErrorCallback = null)
        {
            var go = new GameObject("GeoJSON");
            var layer = go.AddComponent<GeoJSONLayer>();

            if (onErrorCallback != null)
                layer.OnParseError.AddListener(onErrorCallback.Invoke);
            
            layer.PolygonVisualizationMaterial = visualizationMaterial;
            layer.LineRenderer3D = Instantiate(lineRenderer3D);
            layer.ParseGeoJSON(filePath);
            return layer;
        }
    }
}