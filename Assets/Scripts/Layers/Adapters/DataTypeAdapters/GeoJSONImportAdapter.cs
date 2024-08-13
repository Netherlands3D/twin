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
        [SerializeField] private GeoJsonLayerGameObject layerPrefab;
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
            CreateGeoJSONLayer(localFile, displayErrorMessageEvent);
        }

        private void CreateGeoJSONLayer(LocalFile localFile, UnityEvent<string> onErrorCallback = null)
        {
            var fullPath = Path.Combine(Application.persistentDataPath, localFile.LocalFilePath);
            var geoJsonLayerName = Path.GetFileName(localFile.SourceUrl);
            if(localFile.SourceUrl.Length > 0)
                geoJsonLayerName = localFile.SourceUrl;    
        
            //Create a new geojson layer with random color (untill UI provides ways to choose colors)
            GeoJsonLayerGameObject newLayer = Instantiate(layerPrefab);
            newLayer.Name = geoJsonLayerName;
            newLayer.gameObject.name = geoJsonLayerName;
            if (onErrorCallback != null)
                newLayer.OnParseError.AddListener(onErrorCallback.Invoke);

            newLayer.StreamParseGeoJSONFile(fullPath);
        }
    }
}