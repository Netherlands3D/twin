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

            if (expr.IsLiteral)
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
            if (token.Type == JTokenType.Array)
            {
                return DeserializeExpression(token, targetType, serializer);
            }

            return DeserializeLiteralValue(token, targetType, serializer);
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

        /// <summary>
        /// When changing this location - do not forget to change Expr and/or ExpressionEvaluator
        /// </summary>
        private IExpression CastExpressionTo(Type targetType, Operators op, IExpression[] args)
        {
            switch (op)
            {
                case Operators.EqualTo: return Expr.EqualsTo(Expr<ExpressionValue>.TryParse(args[0]), Expr<ExpressionValue>.TryParse(args[1]));
                case Operators.GreaterThan: return Expr.GreaterThan(Expr<ExpressionValue>.TryParse(args[0]), Expr<ExpressionValue>.TryParse(args[1]));
                case Operators.GreaterThanOrEqual: return Expr.GreaterThanOrEqual(Expr<ExpressionValue>.TryParse(args[0]), Expr<ExpressionValue>.TryParse(args[1]));
                case Operators.LessThan: return Expr.LessThan(Expr<ExpressionValue>.TryParse(args[0]), Expr<ExpressionValue>.TryParse(args[1]));
                case Operators.LessThanOrEqual: return Expr.LessThanOrEqual(Expr<ExpressionValue>.TryParse(args[0]), Expr<ExpressionValue>.TryParse(args[1]));
                case Operators.Min: return Expr.Min(Expr<ExpressionValue>.TryParse(args[0]), Expr<ExpressionValue>.TryParse(args[1]));
                case Operators.GetVariable: return Expr.GetVariable(Expr<string>.TryParse(args[0]));
                case Operators.Rgb: return Expr.Rgb(
                    Expr<int>.TryParse(args[0]), 
                    Expr<int>.TryParse(args[1]), 
                    Expr<int>.TryParse(args[2])
                );
            }

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

        private static IExpression DeserializeLiteralValue(JToken token, Type _, JsonSerializer serializer)
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
                        throw new JsonSerializationException($"Unhandled literal type: {token.Type}");
                    }

                    return new Expr<ExpressionValue>(c);
                }
            }
        }
    }
}
