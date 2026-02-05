using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties;
using Netherlands3D.Twin.Layers.Properties;
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

        public override bool CanWrite => false; // This serializer does not implement a custom write function, because we want to use the default seralization settings for writing. We only need it for reading legacy formatted LayerData.  

        public override void WriteJson(JsonWriter writer, LayerData value, JsonSerializer serializer)
        {
            //this is not implemented on purpose, we want to use the default serializer, and therefore this code should never be reached.
            throw new NotImplementedException();
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

            MigratePropertyData(layer, obj);
            
            //styles
            var styleToken = obj["styles"] as JObject;
            if (styleToken != null)
            {
                MigrateSingleLegacyStyle(layer, styleToken, serializer);
            }

            return layer;
        }
        
        private void MigrateSingleLegacyStyle(
            LayerData layer,
            JObject stylesToken,
            JsonSerializer serializer)
        {
            var firstStyle = stylesToken.Properties().FirstOrDefault()?.Value as JObject;
            if (firstStyle == null)
                return;

            var stylingProperty = layer.GetProperty<StylingPropertyData>();
            if(stylingProperty != null) return;

            stylingProperty = new StylingPropertyData();
            
            var metadataToken = firstStyle["metadata"];
            if (metadataToken != null)
            {
                serializer.Populate(metadataToken.CreateReader(), stylingProperty.Metadata);
            }
            var rulesToken = firstStyle["stylingRules"] as JObject;
            if (rulesToken != null)
            {
                stylingProperty.StylingRules.Clear();
                serializer.Populate(rulesToken.CreateReader(), stylingProperty.StylingRules);
            }

            layer.SetProperty(stylingProperty);
        }

        private void MigratePropertyData(LayerData layer, JObject obj)
        {
            var layerProps = obj["layerProperties"] as JArray;
            if (layerProps != null)
            {
                foreach (var prop in layerProps)
                {
                    var type = prop["$type"]?.ToString();

                    // Only handle Annotation type for now
                    if (type == "https://netherlands3d.eu/schemas/projects/layers/properties/Annotation")
                    {
                        var annotationText = prop["annotationText"]?.ToString();

                        var positionToken = prop["position"];
                        Coordinate coordinate;
                        if (positionToken != null)
                        {
                            coordinate = positionToken.ToObject<Coordinate>();
                            
                            var eulerToken = prop["eulerRotation"];
                            Vector3 eulerRotation = Vector3.zero;
                            if (eulerToken != null)
                            {
                                eulerRotation = new Vector3(
                                    eulerToken["x"]?.Value<float>() ?? 0f,
                                    eulerToken["y"]?.Value<float>() ?? 0f,
                                    eulerToken["z"]?.Value<float>() ?? 0f
                                );
                            }

                            var scaleToken = prop["localScale"];
                            Vector3 localScale = Vector3.one;
                            if (scaleToken != null)
                            {
                                localScale = new Vector3(
                                    scaleToken["x"]?.Value<float>() ?? 1f,
                                    scaleToken["y"]?.Value<float>() ?? 1f,
                                    scaleToken["z"]?.Value<float>() ?? 1f
                                );
                            }

                            AnnotationPropertyData annotationProperty = new AnnotationPropertyData(annotationText);
                            TransformLayerPropertyData transformLayerPropertyData = new TransformLayerPropertyData(coordinate, eulerRotation, localScale);
                            layer.SetProperty(annotationProperty);
                            layer.SetProperty(transformLayerPropertyData);
                        }
                    }
                }
            }
        }
    }
}