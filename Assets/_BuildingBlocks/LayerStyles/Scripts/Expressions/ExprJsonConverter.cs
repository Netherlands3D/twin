using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Netherlands3D.LayerStyles.Expressions
{
    public class ExprJsonConverter : JsonConverter
    {
        private static readonly Type ExprOpenGeneric = typeof(Expr<>);

        public override bool CanConvert(Type objectType)
        {
            return typeof(Expr).IsAssignableFrom(objectType);
        }

        public override bool CanWrite => true;
        public override bool CanRead  => true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var expr = (Expr)value;

            if (expr.IsValue)
            {
                // literal → just emit the raw value
                serializer.Serialize(writer, expr.Value);
            }
            else
            {
                // non‐literal → [ operator, arg1, arg2, … ]
                writer.WriteStartArray();
                serializer.Serialize(writer, expr.Operator);
                foreach (var arg in expr.Arguments)
                    serializer.Serialize(writer, arg);
                writer.WriteEndArray();
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            
            // Delegate everything to one recursive method
            return ReadExpression(token, objectType, serializer) as Expr;
        }

        private IExpression ReadExpression(JToken token, Type targetType, JsonSerializer serializer)
        {
            // This should not occur - but if an expression was serialized as an object (as is the case with the
            // old project file (see the <see cref="BoolExpression"/> class); we can pick up on that and immediately
            // return it as an expression
            if (token.Type == JTokenType.Object && token.ToObject<object>(serializer) is IExpression c)
            {
                return c;
            }

            // This is the normal behaviour - if we encounter an array, assume it is an expression
            // (<see href="https://docs.mapbox.com/style-spec/reference/expressions/"/>) and recursively deserialize it.
            if (token.Type == JTokenType.Array)
            {
                return DeserializeExpression(token, targetType, serializer);
            }

            // if it is not an array - let's assume it is a value and attempt to deserialize it as such
            // (<see href="https://docs.mapbox.com/style-spec/reference/expressions/#type-system" />).
            return DeserializeValue(token, targetType, serializer);
        }

        private IExpression DeserializeExpression(JToken token, Type targetType, JsonSerializer serializer)
        {
            var arr = (JArray)token;
            if (arr.Count < 1)
            {
                throw new JsonSerializationException("Expression array must have at least one element");
            }

            // first element is the operator name
            var op = arr[0].ToObject<Operators>(serializer);

            // remaining elements ⇒ recursive call, always producing Expr<ExpressionValue>
            var argType = ExprOpenGeneric.MakeGenericType(typeof(ExpressionValue));
            IExpression[] args = arr
                .Skip(1)
                .Select(t => ReadExpression(t, argType, serializer))
                .ToArray();

            return CastExpressionTo(targetType, op, args);
        }

        private IExpression CastExpressionTo(Type targetType, Operators op, IExpression[] args)
        {
            var e = Expr.Cast(op, args);
            if (e != null) return e;
            
            // Fallback: invoke the internal Expr(string, IExpression[]) ctor; it is preferable to use the factory
            // methods as shown above
            var ctor = targetType.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(string), typeof(IExpression[]) },
                null
            )!;
            return ctor.Invoke(new object[] { op, args }) as IExpression;
        }

        private static IExpression DeserializeValue(JToken token, Type _, JsonSerializer serializer)
        {
            switch (token.Type)
            {
                case JTokenType.Integer: return new Expr<int>(token.Value<long>());
                case JTokenType.Float: return new Expr<double>(token.Value<double>());
                case JTokenType.Boolean: return new Expr<bool>(token.Value<bool>());
                case JTokenType.String: return new Expr<string>(token.Value<string>());
                default:
                {
                    if (token.ToObject<object>(serializer) is not ExpressionValue c)
                    {
                        throw new JsonSerializationException($"Unhandled value type: {token.Type}");
                    }

                    return new Expr<ExpressionValue>(c);
                }
            }
        }
    }
}
