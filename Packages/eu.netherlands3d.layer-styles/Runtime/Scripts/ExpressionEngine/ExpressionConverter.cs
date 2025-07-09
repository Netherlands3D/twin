using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Netherlands3D.LayerStyles.ExpressionEngine
{
    /// <summary>
    /// Custom JsonConverter to read/write Expression structs directly from JSON arrays.
    /// </summary>
    public class ExpressionConverter : JsonConverter<Expression>
    {
        public override Expression ReadJson(JsonReader reader, Type objectType, Expression existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            if (token.Type != JTokenType.Array)
                throw new JsonSerializationException($"Expected JSON array for Expression, got {token.Type}");

            var arr = (JArray)token;
            if (arr.Count < 1)
                throw new JsonSerializationException("Expression array must have at least one element (the operator)");

            // Parse operator via EnumMember
            var opCode = arr[0].ToObject<Expression.Operators>(serializer);

            // Prepare operands
            var operands = new object[arr.Count - 1];
            for (int i = 1; i < arr.Count; i++)
            {
                var element = arr[i];
                operands[i - 1] = element.Type == JTokenType.Array
                    ? element.ToObject<Expression>(serializer)
                    : ((JValue)element).Value;
            }

            return new Expression(opCode, operands);
        }

        public override void WriteJson(JsonWriter writer, Expression value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            // Write operator via EnumMember
            serializer.Serialize(writer, value.Operator);

            // Write operands
            foreach (var operand in value.Operands)
            {
                if (operand is Expression expr)
                    serializer.Serialize(writer, expr);
                else
                    serializer.Serialize(writer, operand);
            }

            writer.WriteEndArray();
        }
    }
}