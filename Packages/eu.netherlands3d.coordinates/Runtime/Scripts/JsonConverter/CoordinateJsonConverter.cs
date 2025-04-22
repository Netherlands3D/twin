using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Scripting;

namespace Netherlands3D.Coordinates
{
    [Preserve]
    public class CoordinateJsonConverter : JsonConverter<Coordinate>
    {
        [Preserve] //needed because we do not want IL2CPP to strip the constructor
        public CoordinateJsonConverter()
        {
            Debug.Log("converter constructor")
        }

        public override Coordinate ReadJson(JsonReader reader, Type objectType, Coordinate existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);

            int coordinateSystem = obj.Value<int>("coordinateSystem");
            double extraLonRot = obj.Value<double?>("extraLongitudeRotation") ?? 0;
            double extraLatRot = obj.Value<double?>("extraLattitudeRotation") ?? 0;
            
            double v1, v2, v3 = 0;
            
            // Check for old format (presence of "Points")
            if (obj["Points"] is JArray pointsArray)
            {
                v1 = pointsArray.Count > 0 ? pointsArray[0].ToObject<double>() : 0;
                v2 = pointsArray.Count > 1 ? pointsArray[1].ToObject<double>() : 0;
                v3 = pointsArray.Count > 2 ? pointsArray[2].ToObject<double>() : 0;
            }
            else
            {
                v1 = obj.Value<double?>("value1") ?? 0;
                v2 = obj.Value<double?>("value2") ?? 0;
                v3 = obj.Value<double?>("value3") ?? 0;
            }

            // New format
            return new Coordinate(coordinateSystem, v1, v2, v3, extraLonRot, extraLatRot);
        }
      
        public override void WriteJson(JsonWriter writer, Coordinate value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("coordinateSystem");
            writer.WriteValue(value.CoordinateSystem);

            writer.WritePropertyName("extraLongitudeRotation");
            writer.WriteValue(value.extraLongitudeRotation);

            writer.WritePropertyName("extraLattitudeRotation");
            writer.WriteValue(value.extraLattitudeRotation);

            writer.WritePropertyName("value1");
            writer.WriteValue(value.value1);

            writer.WritePropertyName("value2");
            writer.WriteValue(value.value2);

            writer.WritePropertyName("value3");
            writer.WriteValue(value.value3);

            writer.WriteEndObject();
        }        
    }
}
