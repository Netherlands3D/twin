using System;
using System.IO;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Projects;
using UnityEngine;
using UnityEngine.Events;
using static Netherlands3D.Functionalities.GeoJSON.LayerPresets.GeoJSONPreset;

namespace Netherlands3D.Twin.DataTypeAdapters
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/GeoJSONImportAdapter", fileName = "GeoJSONImportAdapter", order = 0)]
    public class GeoJSONImportAdapter : ScriptableObject, IDataTypeAdapter
    {
        [SerializeField] private UnityEvent<string> displayErrorMessageEvent = new();

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

        public void Execute(LocalFile localFile)
        {
            ParseGeoJSON(localFile);
        }

        private async void ParseGeoJSON(LocalFile localFile)
        {
            var layerName = CreateName(localFile);
            var url = AssetUriFactory.ConvertLocalFileToAssetUri(localFile);

            var layerData = await App.Layers.Add("geojson", new Args(layerName, url));

            GeoJsonLayerGameObject newLayer = layerData.Reference as GeoJsonLayerGameObject;
            newLayer.Parser.OnParseError.AddListener(displayErrorMessageEvent.Invoke);
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
    }
}