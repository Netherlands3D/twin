using System.IO;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;
using System.Linq;

namespace Netherlands3D.Twin
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/GeoJSONImportAdapter", fileName = "GeoJSONImportAdapter", order = 0)]
    public class GeoJSONImportAdapter : ScriptableObject, IDataTypeAdapter
    {
        [SerializeField] private string[] allowedFileExtensions = new string[] { ".geojson", ".json" };
        [SerializeField] private Material visualizationMaterial;
        [SerializeField] private LineRenderer3D lineRenderer3D;
        [SerializeField] private BatchedMeshInstanceRenderer pointRenderer3D;
        [SerializeField] private UnityEvent<string> displayErrorMessageEvent;

        public bool Supports(LocalFile localFile)
        {
            var extention = Path.GetExtension(localFile.SourceUrl).ToLower();
            if(!allowedFileExtensions.Contains(extention))
                return false;

            //Streamread untill we found some geojson properties
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

        public void Execute(LocalFile localFile)
        {
            throw new System.NotImplementedException();
        }

        public void ParseGeoJSON(string fileName)
        {
            var randomColorVisualisationMaterial = new Material(visualizationMaterial);
            var randomColor = Color.HSVToRGB(Random.value, Random.Range(0.5f, 1f), 1);
            randomColor.a = visualizationMaterial.color.a;
            randomColorVisualisationMaterial.color = randomColor;
            CreateGeoJSONLayer(fileName, randomColorVisualisationMaterial, lineRenderer3D, pointRenderer3D, displayErrorMessageEvent);
        }

        public static GeoJSONLayer CreateGeoJSONLayer(string fileName, Material visualizationMaterial, LineRenderer3D lineRenderer3D, BatchedMeshInstanceRenderer pointRenderer3D, UnityEvent<string> onErrorCallback = null)
        {
            var fullPath = Path.Combine(Application.persistentDataPath, fileName);

            var go = new GameObject(fileName);
            var layer = go.AddComponent<GeoJSONLayer>();

            if (onErrorCallback != null)
                layer.OnParseError.AddListener(onErrorCallback.Invoke);

            layer.SetDefaultVisualizerSettings(visualizationMaterial, lineRenderer3D, pointRenderer3D);
            layer.ParseGeoJSON(fullPath);
            return layer;
        }
    }
}