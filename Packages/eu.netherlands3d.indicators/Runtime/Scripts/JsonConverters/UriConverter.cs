using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Netherlands3D.Indicators.JsonConverters
{
    public class UriConverter : JsonConverter<Uri>
    {
        public override Uri ReadJson(
            JsonReader reader,
            Type objectType,
            Uri existingValue,
            bool hasExistingValue,
            JsonSerializer serializer
        )
        {
            Uri result = default(Uri);

            if (reader.TokenType != JsonToken.String || reader.Value == null) return result;

            return new Uri(reader.Value.ToString());
        }

        public override void WriteJson(JsonWriter writer, Uri value, JsonSerializer serializer)
        {
            writer.WriteValue($"{value}");
        }
    }
}