using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Netherlands3D.OgcApi.JsonConverters
{
    /// <summary>
    /// Deserializes either a single T or an array of T into a T[], and always serializes a T[] as a JSON array.
    /// </summary>
    public class NormalizeToArrayConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(T[]);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);

            return token.Type switch
            {
                JTokenType.Null => null,
                JTokenType.Array => token.ToObject<T[]>(serializer),
                _ => new[] { token.ToObject<T>(serializer) }
            };
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is not T[] array)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartArray();
            foreach (var item in array)
            {
                serializer.Serialize(writer, item);
            }
            writer.WriteEndArray();
        }
    }
}