using System;
using Netherlands3D.SerializableGisExpressions;
using Netherlands3D.SerializableGisExpressions.Operations;
using Newtonsoft.Json.Linq;

namespace Netherlands3D.Catalogs.QueryTranslators
{
    public class ExpressionToCqlJsonTranslator : IQueryTranslator
    {
        public string ToQuery(object value)
        {
            return value switch
            {
                ExpressionValue expressionValue => new JObject(expressionValue.Value).ToString(),
                Expression expression => ToQuery(expression),
                _ => new JObject(value).ToString()
            };
        }

        public string ToQuery(Expression expr)
        {
            //return ExpressionEvaluator.operationMap[expr.Operator.GetType()].ToQuery();
                      
            switch (expr.Operator)
            {
                case AbsOperation:
                    break;


                //case Expression.Operators.Get:
                //    return new JObject { ["property"] = ToQuery(expr.Operands[0]) }
                //        .ToString();
                //case Expression.Operators.In:
                //    var likeExpression = new StringBuilder()
                //        .Append("%")
                //        .Append(ToQuery(expr.Operands[1] + "%"))
                //        .ToString();

                //    return new JObject
                //    {
                //        ["op"] = "like",
                //        ["args"] = new JArray { ToQuery(expr.Operands[0]), likeExpression },
                //        ["nocase"] = true
                //    }.ToString();
                //case Expression.Operators.Array:
                //case Expression.Operators.Boolean:                   
                //case Expression.Operators.Literal:
                //case Expression.Operators.Number:
                //case Expression.Operators.NumberFormat:
                //case Expression.Operators.Object:
                //case Expression.Operators.String:
                //case Expression.Operators.ToBoolean:
                //case Expression.Operators.ToColor:
                //case Expression.Operators.ToNumber:
                //case Expression.Operators.ToString:
                //case Expression.Operators.TypeOf:
                //case Expression.Operators.Not:
                //case Expression.Operators.NotEqual:
                //case Expression.Operators.LessThan:
                //case Expression.Operators.LessThanOrEqual:
                //case Expression.Operators.EqualTo:
                //case Expression.Operators.GreaterThan:
                //case Expression.Operators.GreaterThanOrEqual:
                //case Expression.Operators.All:
                //case Expression.Operators.Any:
                //case Expression.Operators.Hsl:
                //case Expression.Operators.Hsla:
                //case Expression.Operators.Rgb:
                //case Expression.Operators.Rgba:
                //case Expression.Operators.ToHsla:
                //case Expression.Operators.ToRgba:
                //case Expression.Operators.Subtract:
                //case Expression.Operators.Multiply:
                //case Expression.Operators.Divide:
                //case Expression.Operators.Modulo:
                //case Expression.Operators.Power:
                //case Expression.Operators.Add:
                //case Expression.Operators.Abs:
                //case Expression.Operators.Acos:
                //case Expression.Operators.Asin:
                //case Expression.Operators.Atan:
                //case Expression.Operators.Ceil:
                //case Expression.Operators.Cos:
                //case Expression.Operators.E:
                //case Expression.Operators.Floor:
                //case Expression.Operators.Ln:
                //case Expression.Operators.Ln2:
                //case Expression.Operators.Log10:
                //case Expression.Operators.Log2:
                //case Expression.Operators.Max:
                //case Expression.Operators.Min:
                //case Expression.Operators.Pi:
                //case Expression.Operators.Random:
                //case Expression.Operators.Round:
                //case Expression.Operators.Sin:
                //case Expression.Operators.Sqrt:
                //case Expression.Operators.Tan:
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(expr),
                        $"Operator {expr.Operator.ToString()} not supported."
                    );
            }
            return null;
        }
    }
}