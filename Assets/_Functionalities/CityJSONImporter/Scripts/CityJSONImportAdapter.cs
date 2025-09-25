using System;
using System.IO;
using UnityEngine;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Projects;
using Newtonsoft.Json;

namespace Netherlands3D.Functionalities.CityJSON
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/CityJSONImportAdapter", fileName = "CityJSONImportAdapter", order = 0)]
    public class CityJSONImportAdapter : ScriptableObject, IDataTypeAdapter
    {
        public bool Supports(LocalFile localFile)
        {
            using var reader = new StreamReader(localFile.LocalFilePath);
            
            return LooksLikeAJSONFile(reader) && IsCityJson(reader);
        }

        public async void Execute(LocalFile localFile)
        {
            await App.Layers.Add(
                "cityjson", 
                new LayerPresets.CityJSON.Args(
                    CreateName(localFile), 
                    AssetUriFactory.ConvertLocalFileToAssetUri(localFile)
                )
            );
        }

        private bool LooksLikeAJSONFile(StreamReader reader)
        {
            return reader.Peek() is '{' or '[';
        }

        private static bool IsCityJson(StreamReader sr)
        {
            using var jr = new JsonTextReader(sr);
            jr.CloseInput = false;
            jr.DateParseHandling = DateParseHandling.None;
            jr.FloatParseHandling = FloatParseHandling.Double;
            jr.MaxDepth = 2; // we only care about the first object level

            try
            {
                if (!jr.Read() || jr.TokenType != JsonToken.StartObject) return false;

                while (jr.Read())
                {
                    if (jr.TokenType == JsonToken.PropertyName && string.Equals((string)jr.Value, "type", StringComparison.OrdinalIgnoreCase))
                    {
                        return jr.Read() && string.Equals(jr.Value?.ToString(), "CityJSON", StringComparison.OrdinalIgnoreCase);
                    }

                    if (jr.TokenType == JsonToken.EndObject) break;
                }
            }
            catch (JsonReaderException)
            {
                return false; // malformed JSON
            }

            return false;
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