using System;
using System.IO;
using Newtonsoft.Json;

namespace Netherlands3D.DataTypeAdapters
{
    public static class JsonStreamScan
    {
        /// <summary>
        /// Create a configured JsonTextReader for streaming scans.
        /// Leaves the underlying StreamReader open (CloseInput=false).
        /// </summary>
        public static JsonTextReader CreateReader(StreamReader reader)
        {
            var jr = new JsonTextReader(reader)
            {
                CloseInput = false,
                DateParseHandling = DateParseHandling.None,
                FloatParseHandling = FloatParseHandling.Double,
                MaxDepth = null
            };
            return jr;
        }

        /// <summary>
        /// Ensures the current token is StartObject, reading as needed.
        /// Returns false if the next non-whitespace token is not a StartObject.
        /// </summary>
        public static bool TryEnterObject(JsonTextReader jr)
        {
            // If we haven't read anything yet, read the first token
            if (jr.TokenType == JsonToken.None && !jr.Read()) return false;

            // If we're on StartObject already, great. If we're before it, read forward.
            if (jr.TokenType != JsonToken.StartObject)
            {
                // Skip whitespace/comments until we hit a token or EOF
                while (jr.TokenType == JsonToken.Comment || jr.TokenType == JsonToken.None)
                    if (!jr.Read())
                        return false;

                if (jr.TokenType != JsonToken.StartObject) return false;
            }

            return true;
        }
        
        /// <summary>
        /// Ensures the current token is StartArray, reading as needed.
        /// Returns false if the next non-whitespace token is not a StartArray.
        /// </summary>
        public static bool TryEnterArray(JsonTextReader jr)
        {
            if (jr == null) throw new ArgumentNullException(nameof(jr));

            // If we haven't read anything yet, read the first token
            if (jr.TokenType == JsonToken.None && !jr.Read()) return false;

            // Fast-path: already at StartArray
            if (jr.TokenType == JsonToken.StartArray) return true;

            // Skip comments/None until we hit something meaningful or EOF
            while (jr.TokenType == JsonToken.Comment || jr.TokenType == JsonToken.None)
                if (!jr.Read()) return false;

            return jr.TokenType == JsonToken.StartArray;
        }

        /// <summary>
        /// Reads the current object (the one whose current token is StartObject) and
        /// advances until it finds a property named <paramref name="fieldName"/>.
        /// If found, the reader will be positioned on the *value token* of that property
        /// (primitive, StartObject, StartArray, etc.) and returns true.
        ///
        /// If not found, it returns false and leaves the reader positioned just after
        /// the closing '}' of the scanned object.
        ///
        /// Unrelated nested containers are skipped efficiently via jr.Skip().
        /// </summary>
        public static bool TryAdvanceToProperty(
            JsonTextReader jr,
            string fieldName,
            bool caseInsensitive = true)
        {
            if (jr == null) throw new ArgumentNullException(nameof(jr));
            if (string.IsNullOrEmpty(fieldName)) throw new ArgumentNullException(nameof(fieldName));

            if (!TryEnterObject(jr)) return false;

            // We are at StartObject for the object we want to scan
            // Reading once moves inside the object (first property or EndObject)
            if (!jr.Read()) return false;

            while (true)
            {
                // If we reached the end of this object, give up (not found)
                if (jr.TokenType == JsonToken.EndObject) return false;

                if (jr.TokenType == JsonToken.PropertyName)
                {
                    var name = jr.Value as string;
                    // Move to the property's value
                    if (!jr.Read()) return false;

                    bool nameMatches = caseInsensitive
                        ? string.Equals(name, fieldName, StringComparison.OrdinalIgnoreCase)
                        : name == fieldName;

                    if (nameMatches)
                    {
                        // We are now positioned on the value token of the matched property.
                        // Caller can:
                        // - read primitive value directly,
                        // - or if StartObject/StartArray, call this method again to go deeper.
                        return true;
                    }

                    // Not the property we want: if it's a container, skip its entirety.
                    if (jr.TokenType is JsonToken.StartObject or JsonToken.StartArray)
                    {
                        jr.Skip(); // jumps to the matching EndObject/EndArray
                        // After Skip(), the next loop iteration will read the next token.
                        if (!jr.Read()) return false;
                        continue;
                    }

                    // Primitive value (number/string/bool/null) that we don't need -> move on
                    if (!jr.Read()) return false;
                    continue;
                }

                // Defensive: if we encounter a container at this level outside a property
                // (not typical in valid objects), skip it to maintain forward progress.
                if (jr.TokenType is JsonToken.StartObject or JsonToken.StartArray)
                {
                    jr.Skip();
                    if (!jr.Read()) return false;
                    continue;
                }

                // Otherwise, advance
                if (!jr.Read()) return false;
            }
        }

        /// <summary>
        /// From within an array, advances the reader to the next element that is an object.
        /// Leaves the reader positioned on the StartObject token of that element and returns true.
        /// Returns false if no more object elements exist (i.e., we reached EndArray).
        ///
        /// Notes:
        /// - Call this immediately after TryEnterArray (reader on StartArray), or
        ///   after you've finished processing a previous element and called jr.Read() once.
        /// - Non-object elements (primitives or nested arrays) are skipped automatically.
        /// </summary>
        public static bool TryAdvanceToNextObject(JsonTextReader jr)
        {
            if (jr == null) throw new ArgumentNullException(nameof(jr));

            // If we are on StartArray, move into the first element
            if (jr.TokenType == JsonToken.StartArray && !jr.Read()) return false;

            while (true)
            {
                // End of the array => no more elements
                if (jr.TokenType == JsonToken.EndArray) return false;

                // Found an object element
                if (jr.TokenType == JsonToken.StartObject) return true;

                // If we hit a nested array, skip it entirely
                if (jr.TokenType == JsonToken.StartArray)
                {
                    jr.Skip();               // jumps to EndArray
                    if (!jr.Read()) return false;
                    continue;
                }

                // Primitive or anything else in the array: move to next element
                if (!jr.Read()) return false;
            }
        }

        // --- Optional tiny helper for finishing an object and moving on in the same array ---
        /// <summary>
        /// Skips the current container (object/array) and advances once,
        /// positioning the reader on the next token after the container.
        /// </summary>
        public static bool Skip(JsonTextReader jr)
        {
            jr.Skip();
            return jr.Read();
        }

        /// <summary>
        /// Reads the current token as a string (only if the current token is a String).
        /// Returns false otherwise (without moving the reader).
        /// </summary>
        public static bool TryGetCurrentString(JsonTextReader jr, out string value)
        {
            if (jr.TokenType == JsonToken.String && jr.Value is string s)
            {
                value = s;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Convenience: checks a top-level object for a string property with a given name
        /// that matches the provided predicate. Streaming/skip-based, no full DOM load.
        /// </summary>
        public static bool JsonContainsTopLevelFieldWithValue(
            StreamReader reader,
            string fieldName,
            Func<string, bool> predicate,
            bool caseInsensitive = true)
        {
            if (reader == null || string.IsNullOrEmpty(fieldName) || predicate == null) return false;

            using var jr = CreateReader(reader);

            // Ensure we're scanning the top-level object
            if (!TryEnterObject(jr)) return false;

            if (!TryAdvanceToProperty(jr, fieldName, caseInsensitive)) return false;

            // We're positioned on the value of the property; for this helper we require a string
            return TryGetCurrentString(jr, out var s) && predicate(s);
        }
    }
}