using System;
using System.Globalization;
using Netherlands3D.LayerStyles.Expressions;
using Newtonsoft.Json;
using UnityEngine;

namespace Netherlands3D.LayerStyles.Expressions.OperatorHandlers
{
    public static class ToStringOperator
    {
        public const string Code = "to-string";

        public static string Evaluate(Expression expression, ExpressionContext context)
        {
            var o = ExpressionEvaluator.Evaluate(expression, 0, context);
            if (o == null) return "";
            if (o is string s) return s;
            if (o is bool b) return b.ToString().ToLowerInvariant();
            if (ExpressionEvaluator.IsNumber(o))
                return Convert.ToDouble(o, CultureInfo.InvariantCulture)
                    .ToString(CultureInfo.InvariantCulture);
            if (o is Color c)
            {
                int r = Mathf.RoundToInt(c.r * 255f);
                int g = Mathf.RoundToInt(c.g * 255f);
                int b2 = Mathf.RoundToInt(c.b * 255f);
                return $"rgba({r},{g},{b2},{c.a.ToString(CultureInfo.InvariantCulture)})";
            }

            // fallback via JSON stringify
            var json = JsonConvert.SerializeObject(o, Formatting.None);

            // strip wrapping quotes if any
            return json.Length >= 2 && json[0] == '\"' && json[^1] == '\"'
                ? json.Substring(1, json.Length - 2)
                : json;
        }
    }
}