using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties;
using UnityEngine;
using UnityEngine.Scripting;

namespace Netherlands3D
{
    [Preserve]
    public class LayerDataJsonConverter : JsonConverter<LayerData>
    {
        private const string namespaceIdentifier = "https://netherlands3d.eu/schemas/projects/layers/";
        private const string legacyFolderIdentifier = "Folder";
        private const string legacyPolygonIdentifier = "PolygonSelection";

        private const string polygonSelectionVisualizationPrefabID = "0dd48855510674827b667fa4abd5cf60";

        [Preserve]
        public LayerDataJsonConverter()
        {
        }

        public override void WriteJson(JsonWriter writer, LayerData value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            // Create a serializer without THIS converter
            var cleanSerializer = CreateSerializerWithoutSelf(serializer);

            cleanSerializer.Serialize(writer, value);
        }

        public override LayerData ReadJson(
            JsonReader reader,
            Type objectType,
            LayerData existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);

            LayerData layer = new LayerData(obj["name"]?.Value<string>() ?? "Layer");
            //we always load the projectTemplate, so any project files are derived from the project template with the same RootLayer uuid
            if (obj["$type"]?.ToString() == namespaceIdentifier + "Root"
                || obj["$type"]?.ToString() == string.Empty
                || obj["UUID"]?.ToString() == "81797d29-517a-48c8-8733-5dbe7d351dc3")
                layer = new RootLayer("RootLayer");

            Debug.Log("reading layer data: " + layer.Name);
            //Parse as much as default fields as possible
            using (var subReader = obj.CreateReader())
            {
                serializer.Populate(subReader, layer);
            }

            //parse custom fields
            var typeToken = obj["$type"]?.ToString();
            if (typeToken == namespaceIdentifier + legacyFolderIdentifier)
                layer.PrefabIdentifier = "folder";
            else if (typeToken == namespaceIdentifier + legacyPolygonIdentifier)
            {
                layer.PrefabIdentifier = polygonSelectionVisualizationPrefabID;
                var pd = layer.GetProperty<PolygonSelectionLayerPropertyData>();
                var shapeTypeProperty = obj["shapeType"];
                if (shapeTypeProperty != null)
                {
                    Enum.TryParse<ShapeType>(shapeTypeProperty.ToString(), out var shapeType);
                    pd.ShapeType = shapeType;
                }

                var originalPolygonProperty = obj["OriginalPolygon"];
                if (originalPolygonProperty != null)
                {
                    pd.OriginalPolygon = originalPolygonProperty.ToObject<List<Coordinate>>();
                }
                layer.SetProperty(pd);
            }

            return layer;
        }

        private static JsonSerializer CreateSerializerWithoutSelf(JsonSerializer original)
        {
            var serializer = new JsonSerializer
            {
                ContractResolver = original.ContractResolver,
                Culture = original.Culture,
                DateFormatHandling = original.DateFormatHandling,
                DateParseHandling = original.DateParseHandling,
                DateTimeZoneHandling = original.DateTimeZoneHandling,
                FloatFormatHandling = original.FloatFormatHandling,
                FloatParseHandling = original.FloatParseHandling,
                Formatting = original.Formatting,
                MaxDepth = original.MaxDepth,
                MissingMemberHandling = original.MissingMemberHandling,
                NullValueHandling = original.NullValueHandling,
                ObjectCreationHandling = original.ObjectCreationHandling,
                PreserveReferencesHandling = original.PreserveReferencesHandling,
                ReferenceLoopHandling = original.ReferenceLoopHandling,
                StringEscapeHandling = original.StringEscapeHandling,
                TypeNameHandling = original.TypeNameHandling
            };

            foreach (var c in original.Converters)
            {
                if (c is not LayerDataJsonConverter)
                    serializer.Converters.Add(c);
            }

            return serializer;
        }
    }
}