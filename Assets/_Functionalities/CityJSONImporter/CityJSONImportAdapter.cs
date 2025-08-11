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

            //todo, we should check against a schema for optimization
            while (jsonReader.Read())
            {
                if (jsonReader.TokenType == JsonToken.PropertyName && (string)jsonReader.Value == "type")
                {
                    jsonReader.Read(); //reads the value of the type object
                    if ((string)jsonReader.Value == "CityJSON")
                        return true;
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