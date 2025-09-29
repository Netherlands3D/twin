using System;
using System.IO;
using Newtonsoft.Json;

namespace Netherlands3D.DataTypeAdapters
{
    /// <summary>
    /// Helper class with commonly used matchers that can be used in the "Supports" method to detect the type of file.
    /// </summary>
    public static class ContentMatches
    {
        /// <summary>
        /// Is the content -passed as a streamreader- a JSON object, and does it have a field whose value matches the
        /// given predicate?
        /// </summary>
        public static bool JsonContainsTopLevelFieldWithValue(
            StreamReader reader, 
            string fieldName, 
            Func<string, bool> predicate, 
            bool caseInsensitive = true
        ) {
            if (reader == null || string.IsNullOrEmpty(fieldName) || predicate == null)
                return false;

            if (!JsonObject(reader)) return false;

            using var jr = new JsonTextReader(reader);
            jr.CloseInput = false;
            jr.DateParseHandling = DateParseHandling.None;
            jr.FloatParseHandling = FloatParseHandling.Double;
            jr.MaxDepth = null;

            // if the first token in the content is not a '{', then there is something wrong
            if (!jr.Read() || jr.TokenType != JsonToken.StartObject) return false;

            while (jr.Read())
            {
                // if the content is a '}' without having found our predicate, then this is a negative (please note,
                // we skip nested content, so the '}' matches the closing brace of the whole object)
                if (jr.TokenType == JsonToken.EndObject) return false;
                
                // Not a property name? Not of interest to perform checks, let's move on
                if (jr.TokenType != JsonToken.PropertyName) continue;

                var name = (string)jr.Value;
                if (!jr.Read()) break;

                // is this the property that we are looking for?
                bool nameMatches = caseInsensitive
                    ? string.Equals(name, fieldName, StringComparison.OrdinalIgnoreCase)
                    : name == fieldName;

                // is this a matching value? Then we approve!
                if (jr.TokenType == JsonToken.String && nameMatches && jr.Value is string s && predicate(s))
                    return true;

                // Do we encounter a nested structure (object or array), skip it. We only check the top level
                if (jr.TokenType is JsonToken.StartObject or JsonToken.StartArray) jr.Skip();
            }

            return false;
        }

        /// <summary>
        /// Is the content -probably- a JSON Object?
        ///
        /// This will move the streamreader past the Byte Order Marker (BOM) and any whitespace, but keeps the opening
        /// object character.
        /// </summary>
        public static bool JsonObject(StreamReader sr)
        {
            SkipBomAndWhitespace(sr);
            return sr.Peek() == '{';
        }

        /// <summary>
        /// Is the content -probably- a JSON Array?
        ///
        /// This will move the streamreader past the Byte Order Marker (BOM) and any whitespace, but keeps the opening
        /// array character.
        /// </summary>
        public static bool JsonArray(StreamReader sr)
        {
            SkipBomAndWhitespace(sr);
            return sr.Peek() == '[';
        }

        private static void SkipBomAndWhitespace(TextReader tr)
        {
            while (true)
            {
                int c = tr.Peek();
                if (c == -1) break;
                if (c == '\uFEFF' || char.IsWhiteSpace((char)c)) { tr.Read(); continue; }
                break;
            }
        }
    }
}