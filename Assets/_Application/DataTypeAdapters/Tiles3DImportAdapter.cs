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
            var layerParent = GameObject.FindWithTag("3DTileParent").transform;
            var newObject = Instantiate(layerPrefab, Vector3.zero, layerPrefab.transform.rotation, layerParent);

            var layerComponent = newObject.gameObject.GetComponent<Tile3DLayerGameObject>();
            if (!layerComponent)
                layerComponent = newObject.gameObject.AddComponent<Tile3DLayerGameObject>();

            layerComponent.Name = layerPrefab.name;
            layerComponent.PropertyData.Url = localFile.SourceUrl; //set url to get tiles
        }

        public bool Supports(LocalFile localFile)
        {
            //TODO, check if reading the geojson check is potentially very large, maybe a timeout, maybe putting the tile3d import adapter


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