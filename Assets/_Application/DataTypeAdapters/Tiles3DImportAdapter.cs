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
            // TODO: check if reading the geojson is potentially very large, maybe a timeout or a schema
            // https://github.com/CesiumGS/3d-tiles/blob/main/specification/schema/tileset.schema.json

            // TODO: Overweeg om efficiënter en explicieter te parsen/valideren:
            // 1. JObject.Parse (Newtonsoft): volledige JSON inlezen en gericht properties controleren
            //    var json = File.ReadAllText(path);
            //    var root = JObject.Parse(json);
            //    if (root["asset"]?["version"]?.ToString() != null) ...
            //
            // 2. System.Text.Json (sneller, lichter alternatief voor Newtonsoft):
            //    var json = File.ReadAllText(path);
            //    using var doc = JsonDocument.Parse(json);
            //    if (doc.RootElement.TryGetProperty("asset", out var asset)) ...
            //
            // 3. JSON Schema validatie (bijv. met Newtonsoft.Json.Schema):
            //    JSchema schema = JSchema.Parse(File.ReadAllText("tileset.schema.json"));
            //    JObject obj = JObject.Parse(File.ReadAllText(path));
            //    bool isValid = obj.IsValid(schema, out IList<string> errors);
            //
            // Let op: bij grote bestanden of veel validaties kan performance relevant zijn,
            // dus alleen schema-validatie gebruiken als strikte structuurcontrole gewenst is.

            using var reader = new StreamReader(localFile.LocalFilePath);
            using var jsonReader = new JsonTextReader(reader);

            if (!jsonReader.Read())
                return false; // Bestand is leeg of ongeldig

            bool isStartOfValidJson = jsonReader.TokenType == JsonToken.StartObject || jsonReader.TokenType == JsonToken.StartArray;
            if (!isStartOfValidJson)
                return false;

            // Nu verder lezen naar inhoud
            while (jsonReader.Read())
            {
                if (jsonReader.TokenType == JsonToken.PropertyName && (string)jsonReader.Value == "type")
                {
                    jsonReader.Read(); // naar value
                    if ((string)jsonReader.Value == "FeatureCollection" || (string)jsonReader.Value == "Feature")
                        return false; // GeoJSON, geen 3D Tileset
                }

                if (jsonReader.TokenType == JsonToken.PropertyName && (string)jsonReader.Value == "asset")
                {
                    jsonReader.Read(); // StartObject
                    if (jsonReader.TokenType == JsonToken.StartObject)
                    {
                        while (jsonReader.Read() && jsonReader.TokenType != JsonToken.EndObject)
                        {
                            if (jsonReader.TokenType == JsonToken.PropertyName && (string)jsonReader.Value == "version")
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}