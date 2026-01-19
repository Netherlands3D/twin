using System;
using System.IO;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Functionalities.GeoJSON.LayerPresets;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Services;
using UnityEngine;

namespace Netherlands3D.Twin.DataTypeAdapters
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/GeoJSONImportAdapter", fileName = "GeoJSONImportAdapter", order = 0)]
    public class GeoJSONImportAdapter : ScriptableObject, IDataTypeAdapter<Layer>
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

        public Layer Execute(LocalFile localFile)
        {
            return ParseGeoJSON(localFile);
        }

        private Layer ParseGeoJSON(LocalFile localFile)
        {
            var layerName = CreateName(localFile);
            var url = AssetUriFactory.ConvertLocalFileToAssetUri(localFile);

            return App.Layers.Add(new GeoJSONPreset.Args(layerName, url));
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