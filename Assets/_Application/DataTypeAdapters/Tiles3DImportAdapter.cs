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

        public void Execute(LocalFile localFile)
        {
            var newObject = Instantiate(layerPrefab, Vector3.zero, layerPrefab.transform.rotation);

            if (!newObject.gameObject.TryGetComponent<Tile3DLayerGameObject>(out var layerComponent))
            {
                throw new MissingComponentException("Missing the Tile3DLayerGameObject component!");
            }

            layerComponent.Name = layerPrefab.name;
            layerComponent.PropertyData.Url = localFile.SourceUrl; //set url to get tiles
        }

        public bool Supports(LocalFile localFile)
        {
            //TODO, check if reading the geojson check is potentially very large, maybe a timeout or a schema https://github.com/CesiumGS/3d-tiles/blob/main/specification/schema/tileset.schema.json


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
                    jsonReader.Read(); //reads value
                    if ((string)jsonReader.Value == "FeatureCollection" || (string)jsonReader.Value == "Feature")
                        return false; //this is a GeoJson, not a 3D Tileset
                }

                if (jsonReader.TokenType == JsonToken.PropertyName && (string)jsonReader.Value == "asset")
                {
                    jsonReader.Read(); //reads StartObject {
                    jsonReader.Read(); //reads new object key which should be the version
                    if ((string)jsonReader.Value == "version")
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
    }
}