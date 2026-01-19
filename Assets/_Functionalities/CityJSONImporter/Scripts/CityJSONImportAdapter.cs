using System;
using System.IO;
using UnityEngine;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Services;

namespace Netherlands3D.Functionalities.CityJSON
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/CityJSONImportAdapter", fileName = "CityJSONImportAdapter", order = 0)]
    public class CityJSONImportAdapter : ScriptableObject, IDataTypeAdapter<Layer>
    {
        public bool Supports(LocalFile localFile)
        {
            using var reader = new StreamReader(localFile.LocalFilePath);
            
            return ContentMatches.JsonObject(reader) 
                && ContentMatches.JsonContainsTopLevelFieldWithValue(
                    reader, 
                    "type",
                    value => value.StartsWith("CityJSON", StringComparison.OrdinalIgnoreCase)
                );
        }

        public Layer Execute(LocalFile localFile)
        {
            var preset = new LayerPresets.CityJSONPreset.Args(
                CreateName(localFile),
                AssetUriFactory.ConvertLocalFileToAssetUri(localFile)
            );
            return App.Layers.Add(preset);
        }

        private static string CreateName(LocalFile f)
        {
            var s = string.IsNullOrWhiteSpace(f.SourceUrl) ? f.LocalFilePath : f.SourceUrl;

            // If it's a URL, use its local path (no query/fragment); else leave as-is.
            if (Uri.TryCreate(s, UriKind.RelativeOrAbsolute, out var uri) && uri.IsAbsoluteUri)
                s = uri.LocalPath.TrimEnd('/');

            var file = Path.GetFileName(s);
            return string.IsNullOrEmpty(file) ? "CityJSON Layer" : Path.GetFileNameWithoutExtension(file);
        }
    }
}