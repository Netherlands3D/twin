using System;
using System.IO;
using UnityEngine;
using Netherlands3D.DataTypeAdapters;
using Newtonsoft.Json;

namespace Netherlands3D.Functionalities.CityJSON
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/CityJSONImportAdapter", fileName = "CityJSONImportAdapter", order = 0)]
    public class CityJSONImportAdapter : ScriptableObject, IDataTypeAdapter
    {
        [SerializeField] private CityJSONSpawner layerPrefab;

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
                if (jsonReader.TokenType == JsonToken.PropertyName &&
                    string.Equals((string)jsonReader.Value, "type", StringComparison.OrdinalIgnoreCase))
                {
                    jsonReader.Read(); //read the value of the "type" property
                    string typeValue = jsonReader.Value?.ToString();

                    if (string.Equals(typeValue, "CityJSON", StringComparison.OrdinalIgnoreCase))
                        return true; //this is a cityjson, not a geojson

                    if (typeValue.Equals("FeatureCollection", StringComparison.OrdinalIgnoreCase) ||
                        typeValue.Equals("Feature", StringComparison.OrdinalIgnoreCase))
                        return false; //this is a geojson, not a cityjson
                }

                if (jsonReader.TokenType == JsonToken.PropertyName &&
                    string.Equals((string)jsonReader.Value, "asset", StringComparison.OrdinalIgnoreCase))
                {
                    jsonReader.Read(); //reads StartObject {
                    jsonReader.Read(); //reads new object key which should be the version
                    if (string.Equals((string)jsonReader.Value, "version", StringComparison.OrdinalIgnoreCase))
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
            var fullPath = localFile.LocalFilePath;
            var fileName = Path.GetFileName(fullPath);
            CityJSONSpawner newLayer = Instantiate(layerPrefab);
            newLayer.gameObject.name = fileName;

            newLayer.SetCityJSONPathInPropertyData(fullPath);
        }
    }
}