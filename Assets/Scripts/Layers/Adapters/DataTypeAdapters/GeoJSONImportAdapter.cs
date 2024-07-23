using System.IO;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;
using Netherlands3D.Twin.Layers;

namespace Netherlands3D.Twin
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/GeoJSONImportAdapter", fileName = "GeoJSONImportAdapter", order = 0)]
    public class GeoJSONImportAdapter : ScriptableObject, IDataTypeAdapter
    {
        [SerializeField] private Material visualizationMaterial;
        [SerializeField] private LineRenderer3D lineRenderer3D;
        [SerializeField] private BatchedMeshInstanceRenderer pointRenderer3D;
        [SerializeField] private UnityEvent<string> displayErrorMessageEvent;

        public bool Supports(LocalFile localFile)
        {
            // Check if the file has JSON content
            if (!LooksLikeAJSONFile(localFile.LocalFilePath))
                return false;

            // Streamread the JSON until we find some GeoJSON properties
            using var reader = new StreamReader(localFile.LocalFilePath);
            using var jsonReader = new JsonTextReader(reader);

            while (jsonReader.Read())
            {
                if (jsonReader.TokenType == JsonToken.PropertyName && (string)jsonReader.Value == "type")
                {
                    jsonReader.Read();
                    if ((string)jsonReader.Value == "FeatureCollection" || (string)jsonReader.Value == "Feature")
                        return true;
                }
            }

            return true;
        }

        private bool LooksLikeAJSONFile(string filePath)
        {
            using var reader = new StreamReader(filePath);
            var firstChar = reader.Read();
            return firstChar == '{' || firstChar == '[';
        }

        public void Execute(LocalFile localFile)
        {
            ParseGeoJSON(localFile);
        }

        public void ParseGeoJSON(LocalFile localFile)
        {
            var randomColorVisualisationMaterial = new Material(visualizationMaterial);
            var randomColor = Color.HSVToRGB(Random.value, Random.Range(0.5f, 1f), 1);
            randomColor.a = visualizationMaterial.color.a;
            randomColorVisualisationMaterial.color = randomColor;
            CreateGeoJSONLayer(localFile, randomColorVisualisationMaterial, lineRenderer3D, pointRenderer3D, displayErrorMessageEvent);
        }

        public static GeoJSONLayer CreateGeoJSONLayer(LocalFile localFile, Material visualizationMaterial, LineRenderer3D lineRenderer3D, BatchedMeshInstanceRenderer pointRenderer3D, UnityEvent<string> onErrorCallback = null)
        {
            var fullPath = Path.Combine(Application.persistentDataPath, localFile.LocalFilePath);
            var geoJsonLayerName = Path.GetFileName(localFile.SourceUrl);

            if(localFile.SourceUrl.Length > 0)
                geoJsonLayerName = localFile.SourceUrl;    

            var go = new GameObject(geoJsonLayerName);
            var layer = go.AddComponent<GeoJSONLayer>();

            if (onErrorCallback != null)
                layer.OnParseError.AddListener(onErrorCallback.Invoke);

            layer.SetDefaultVisualizerSettings(visualizationMaterial, lineRenderer3D, pointRenderer3D);
            layer.StreamParseGeoJSON(fullPath);
            return layer;
        }
    }
}