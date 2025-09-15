using System.IO;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Projects;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using static Netherlands3D.Functionalities.GeoJSON.LayerPresets.GeoJSON;

namespace Netherlands3D.Twin.DataTypeAdapters
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/GeoJSONImportAdapter", fileName = "GeoJSONImportAdapter", order = 0)]
    public class GeoJSONImportAdapter : ScriptableObject, IDataTypeAdapter
    {
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

        private async void ParseGeoJSON(LocalFile localFile)
        {
            var layerName = CreateName(localFile);
            var url = AssetUriFactory.ConvertLocalFileToAssetUri(localFile);

            var layerData = await App.Layers.Add("geojson", new Args(layerName, url));

            GeoJsonLayerGameObject newLayer = layerData.Reference as GeoJsonLayerGameObject;
            
            // TODO: double check if the title in the args above don't already do this?
            newLayer.gameObject.name = layerName;
            if (displayErrorMessageEvent != null)
            {
                newLayer.Parser.OnParseError.AddListener(displayErrorMessageEvent.Invoke);
            }
        }

        private static string CreateName(LocalFile localFile)
        {
            var geoJsonLayerName = Path.GetFileName(localFile.SourceUrl);
            if (localFile.SourceUrl is { Length: > 0 })
            {
                geoJsonLayerName = localFile.SourceUrl;
            }

            return geoJsonLayerName;
        }
    }
}