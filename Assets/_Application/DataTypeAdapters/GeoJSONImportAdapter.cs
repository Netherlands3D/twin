using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Functionalities.GeoJSON.LayerPresets;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Projects;
using Newtonsoft.Json;
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

            var root = JObject.Parse(bodyContents);
            var features = root["features"] as JArray;

            var geoJsonLayerPreset = new GeoJSONPreset.Args(layerName, url);
            var presets = new List<LayerPresetArgs>();

            var uniquePresets = new Dictionary<string, List<LayerPresetArgs>>();

            if (features != null)
            {
                var serializer = JsonSerializer.CreateDefault();

                foreach (var featureToken in features)
                {
                    var properties = featureToken["properties"];
                    var featureType = properties?["featureType"]?.Value<string>();

                    if (string.IsNullOrEmpty(featureType))
                        continue;

                    using var reader = featureToken.CreateReader();
                    var feature = serializer.Deserialize<Feature>(reader);

                    if (feature?.Geometry is not Point point)
                        continue;

                    var crs = GeoJSONParser.GetCoordinateSystem(feature.CRS);
                    var coordinate = GeometryVisualizationFactory.ConvertToCoordinate(crs, point.Coordinates);

                    switch (featureType)
                    {
                        case "Annotation":
                        {
                            var annotation = properties.ToObject<Annotation>();
                            ColorUtility.TryParseHtmlString(annotation.Color, out Color color);

                            presets.Add(new GeoJSONAnnotationPreset.Args(
                                coordinate,
                                annotation.Title,
                                annotation.AnnotationText,
                                annotation.ImageUrl,
                                annotation.ImageCaption,
                                color));

                            break;
                        }

                        case "ElectricityPole":
                        {
                            var pole = properties.ToObject<ConnectableElectricityPole>();


                            string trajectType = pole.OBJECTIDENTIFIER.Split(" ")[0];
                            
                            if (!uniquePresets.TryGetValue(trajectType, out var polePresets))
                            {
                                polePresets = new List<LayerPresetArgs>();
                                uniquePresets[trajectType] = polePresets;
                            }

                            
                            var folderPreset = new FolderPreset.Args(trajectType);
                            
                            polePresets.Add(new GeoJSONElectricityPolePreset.Args(
                                coordinate,
                                pole.OBJECTIDENTIFIER,
                                pole.Type));
                            break;
                        }

                        default:
                            Debug.LogWarning(
                                $"Unsupported GeoJSON feature type: {featureType}");
                            break;
                    }
                }
            }
            
            var children = new List<LayerPresetNode>();
            children.AddRange(presets.Select(preset => new LayerPresetNode { preset = preset }));
            foreach (var group in uniquePresets)
            {
                LayerPresetNode childNode = new LayerPresetNode
                {
                    preset = new FolderPreset.Args(group.Key),
                    children = group.Value.Select(preset => new LayerPresetNode { preset = preset }).ToArray()
                };
                children.Add(childNode);
            }
            return LayerPresetResult.Create(new LayerPresetNode { preset = geoJsonLayerPreset, children = children.ToArray() });
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
            public Coordinate Coordinate { get; set; }
        }

        public struct ConnectableElectricityPole
        {
            public string OBJECTIDENTIFIER { get; set; }
            public string Type { get; set; }
            public string featureType { get; set; }
        }
    }
}