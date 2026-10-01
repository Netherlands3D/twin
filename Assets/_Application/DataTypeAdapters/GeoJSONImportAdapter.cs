using System;
using System.Collections.Generic;
using System.IO;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Functionalities.GeoJSON.LayerPresets;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Projects;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Netherlands3D.Twin.DataTypeAdapters
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/GeoJSONImportAdapter", fileName = "GeoJSONImportAdapter", order = 0)]
    public class GeoJSONImportAdapter : ScriptableObject, IDataTypeAdapter<LayerPresetResult>
    {
        public bool Supports(LocalFile localFile)
        {
            using var reader = new StreamReader(localFile.LocalFilePath);
            
            return ContentMatches.JsonObject(reader) 
                && ContentMatches.JsonContainsTopLevelFieldWithValue(
                   reader, 
                   "type",
                   value => value.Equals("Feature", StringComparison.OrdinalIgnoreCase) 
                        || value.Equals("FeatureCollection", StringComparison.OrdinalIgnoreCase)
                );
        }

        public LayerPresetResult Execute(LocalFile localFile)
        {
            var layerName = CreateName(localFile);
            var url = AssetUriFactory.ConvertLocalFileToAssetUri(localFile);

            var bodyContents = File.ReadAllText(localFile.LocalFilePath);

            Annotation[] annotations = ParseAnnotations(bodyContents);
            var geoJsonLayerPreset = new GeoJSONPreset.Args(layerName, url);
            var presets = new LayerPresetArgs[annotations.Length];
            for (int i = 0; i < annotations.Length; i++)
            {
                var annotation = annotations[i];
                Debug.Log("Adding Annotation preset for featureType: " + annotation);
                ColorUtility.TryParseHtmlString(annotation.Color, out Color color);
                presets[i] = new GeoJSONAnnotationPreset.Args(annotation.Title, annotation.AnnotationText, annotation.ImageUrl, annotation.ImageCaption,  color, true);
            }
            return new LayerPresetResult() { parent = geoJsonLayerPreset, children = presets };
        }
        
        private static Annotation[] ParseAnnotations(string bodyContents)
        {
            var root = JObject.Parse(bodyContents);

            var features = root["features"] as JArray;
            if (features == null)
                return Array.Empty<Annotation>();

            var annotations = new List<Annotation>();

            foreach (var feature in features)
            {
                var annotation = feature["properties"]?["annotation"];

                if (annotation is not JObject annotationObject)
                    continue;

                annotations.Add(new Annotation
                {
                    Title = annotationObject.Value<string>("title"),
                    AnnotationText = annotationObject.Value<string>("annotationText"),
                    ImageUrl = annotationObject.Value<string>("imageUrl"),
                    ImageCaption = annotationObject.Value<string>("imageCaption"),
                    Color = annotationObject.Value<string>("color")
                });
            }

            return annotations.ToArray();
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
        
        public struct Annotation
        {
            public string Title { get; set; }
            public string AnnotationText { get; set; }
            public string ImageUrl { get; set; }
            public string ImageCaption { get; set; }
            public string Color { get; set; }
        }
    }
}