using System.IO;
using Netherlands3D._Application._Twin.SDK;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes;
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

        private async void CreateGeoJSONLayer(LocalFile localFile, UnityEvent<string> onErrorCallback = null)
        {
            var geoJsonLayerName = Path.GetFileName(localFile.SourceUrl);
            if (localFile.SourceUrl is { Length: > 0 })
            {
                geoJsonLayerName = localFile.SourceUrl;
            }

            var randomLayerColor = LayerColor.Random();

            var symbolizer = new Symbolizer();
            symbolizer.SetFillColor(randomLayerColor);
            symbolizer.SetStrokeColor(randomLayerColor);
            
            var layerData = await Sdk.Layers.Add(
                Layer.OfType(layerPrefab.PrefabIdentifier)
                    .NamedAs(geoJsonLayerName)
                    .WithColor(randomLayerColor)
                    .SetDefaultStyling(symbolizer)
            );

            GeoJsonLayerGameObject newLayer = layerData.Reference as GeoJsonLayerGameObject;
            newLayer.gameObject.name = geoJsonLayerName;
            if (onErrorCallback != null)
            {
                newLayer.Parser.OnParseError.AddListener(onErrorCallback.Invoke);
            }

            var localPath = localFile.LocalFilePath;
            var propertyData = newLayer.PropertyData as LayerURLPropertyData;
            propertyData.Data = localFile.SourceUrl.StartsWith("http") 
                ? AssetUriFactory.CreateRemoteAssetUri(localFile.SourceUrl) 
                : AssetUriFactory.CreateProjectAssetUri(localPath);
        }
    }
}