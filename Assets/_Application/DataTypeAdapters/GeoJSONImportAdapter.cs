using System.IO;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;

namespace Netherlands3D.Twin.DataTypeAdapters
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


            //todo, we should check against a schema for optimization https://geojson.org/schema/GeoJSON.json
            while (jsonReader.Read())
            {
                if (jsonReader.TokenType == JsonToken.PropertyName && (string)jsonReader.Value == "type")
                {
                    jsonReader.Read(); //reads the value of the type object
                    if ((string)jsonReader.Value == "FeatureCollection" || (string)jsonReader.Value == "Feature")
                        return true;
                }

                if (jsonReader.TokenType == JsonToken.PropertyName && (string)jsonReader.Value == "asset")
                {
                    jsonReader.Read(); //reads StartObject {
                    jsonReader.Read(); //reads new object key which should be the version
                    if ((string)jsonReader.Value == "version")
                        return false; //this is a 3D Tileset, not a GeoJson
                }
            }

            return false;
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
            var localFilePath = Path.Combine(Application.persistentDataPath, localFile.LocalFilePath);
            var geoJsonLayerName = Path.GetFileName(localFile.SourceUrl);
            if(localFile.SourceUrl.Length > 0)
                geoJsonLayerName = localFile.SourceUrl;    
        
            GeoJsonLayerGameObject newLayer = Instantiate(layerPrefab);
            newLayer.Name = geoJsonLayerName;
            newLayer.gameObject.name = geoJsonLayerName;
            if (onErrorCallback != null)
                newLayer.Parser.OnParseError.AddListener(onErrorCallback.Invoke);

            //GeoJSON layer+visual colors are set to random colors until user can pick colors in UI
            var randomLayerColor = Color.HSVToRGB(UnityEngine.Random.value, UnityEngine.Random.Range(0.5f, 1f), 1);
            randomLayerColor.a = 0.5f;
            newLayer.LayerData.Color = randomLayerColor;
            
            var symbolizer = newLayer.LayerData.DefaultSymbolizer;
            symbolizer?.SetFillColor(randomLayerColor);
            symbolizer?.SetStrokeColor(randomLayerColor);
            
            var localPath = localFile.LocalFilePath;
            var propertyData = newLayer.PropertyData as LayerURLPropertyData;
            propertyData.Data = localFile.SourceUrl.StartsWith("http") 
                ? AssetUriFactory.CreateRemoteAssetUri(localFile.SourceUrl) 
                : AssetUriFactory.CreateProjectAssetUri(localPath);
        }
    }
}