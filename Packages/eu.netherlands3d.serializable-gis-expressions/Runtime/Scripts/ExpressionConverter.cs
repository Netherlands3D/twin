using System;
using Netherlands3D.SerializableGisExpressions.Operations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Scripting;

namespace Netherlands3D.SerializableGisExpressions
{
    /// <summary>
    /// Custom JsonConverter to read/write Expression structs directly from JSON arrays.
    /// </summary>
    [Preserve]
    public class ExpressionConverter : JsonConverter<Expression>
    {
        public override Expression ReadJson(JsonReader reader, Type objectType, Expression existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            switch (token.Type)
            {
                case JTokenType.Boolean:
                    return Expression.Boolean(((JValue)token).Value);
                case JTokenType.Null:
                    // if the token type is a null, we accept that as a "*", or better said: always true. The default
                    // styling rule is an example of a rule that always matches.
                    return Expression.Boolean(true);
                case JTokenType.Object:
                    return BackwardsCompatibilityWithOldBooleanExpression(token);
                case JTokenType.Array:
                    break;
                default:
                    throw new JsonSerializationException($"Expected JSON array for Expression, got {token.Type}");
            }

            var arr = (JArray)token;
            if (arr.Count < 1)
            {
                throw new JsonSerializationException("Expression array must have at least one element (the operator)");
            }

            // Parse operator via EnumMember
            var opCode = arr[0].ToObject<IOperation>(serializer);

            // Prepare operands
            var operands = new object[arr.Count - 1];
            for (int i = 1; i < arr.Count; i++)
            {
                var element = arr[i];
                if (element.Type == JTokenType.Array)
                {
                    operands[i - 1] = element.ToObject<Expression>(serializer);
                }
                else
                {
                    operands[i - 1] = ((JValue)element).Value;
                }
            }

            return new Expression(opCode, operands);
        }

        private static Expression BackwardsCompatibilityWithOldBooleanExpression(JToken token)
        {
            // In the first version of the expression library, the boolean expression was represented as an object;
            // some projects remain that use this notation. Since we moved on from that implementation quickly,
            // no other type was in use other than the default boolean expression.
            var tokenObject = (JObject)token;
            tokenObject.TryGetValue("$type", out var typeToken);
            if (typeToken?.Value<string>() !=
                "https://netherlands3d.eu/schemas/projects/layers/styling/expressions/Bool")
            {
                return Expression.Boolean(false);
            }
                
            tokenObject.TryGetValue("value", out var valueToken);
            return Expression.Boolean(valueToken?.Value<bool>() ?? false);
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