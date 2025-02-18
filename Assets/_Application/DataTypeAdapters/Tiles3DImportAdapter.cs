using UnityEngine;
using UnityEngine.Events;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Functionalities.OGC3DTiles;
using System.IO;
using Newtonsoft.Json;

namespace Netherlands3D.Twin.DataTypeAdapters
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/Tiles3DImportAdapter", fileName = "Tiles3DImportAdapter", order = 0)]
    public class Tiles3DImportAdapter : ScriptableObject, IDataTypeAdapter
    {
        [SerializeField] private Tile3DLayerGameObject layerPrefab;
        [SerializeField] private UnityEvent<string> displayErrorMessageEvent;

        public void Execute(LocalFile localFile)
        {
            
        }

        public bool Supports(LocalFile localFile)
        {
            // Check if the file has JSON content
            if (!LooksLikeA3DTileset(localFile.LocalFilePath))
                return false;

            // Stream-read the JSON to find 3D Tileset properties
            using var reader = new StreamReader(localFile.LocalFilePath);
            using var jsonReader = new JsonTextReader(reader);

            bool inAssetObject = false;
            bool foundVersion = false;

            while (jsonReader.Read())
            {
                if (jsonReader.TokenType == JsonToken.PropertyName)
                {
                    string propertyName = (string)jsonReader.Value;

                    if (propertyName == "asset")
                        inAssetObject = true;  // Entering "asset" object

                    if (inAssetObject && propertyName == "version")
                    {
                        jsonReader.Read();
                        if (jsonReader.TokenType == JsonToken.String) // Ensure "version" is a string
                            foundVersion = true;
                    }
                }

                // If we finished reading the "asset" object and found "version", confirm it's a 3D Tileset
                if (foundVersion)
                    return true;
            }

            return false;
        }

        private bool LooksLikeA3DTileset(string filePath)
        {
            using var reader = new StreamReader(filePath);
            int linesToCheck = 10; // Read up to 10 lines to find "asset" and "version"

            while (!reader.EndOfStream && linesToCheck-- > 0)
            {
                string line = reader.ReadLine();
                if (line != null && line.Contains("\"asset\"") && line.Contains("\"version\""))
                    return true;
            }

            return false;
        }
    }
}