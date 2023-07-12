using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Netherlands3D.Indicators.JsonConverters
{
    public class ColorConverter : JsonConverter<Color>
    {
        public override Color ReadJson(
            JsonReader reader,
            Type objectType,
            Color existingValue,
            bool hasExistingValue,
            JsonSerializer serializer
        )
        {
            Color result = default(Color);

            if (reader.TokenType != JsonToken.String) return result;

            ColorUtility.TryParseHtmlString(reader.Value?.ToString(), out result);

            return result;
        }

        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
        {
            writer.WriteValue($"#{ColorUtility.ToHtmlStringRGBA(value)}");
        }
    }
}