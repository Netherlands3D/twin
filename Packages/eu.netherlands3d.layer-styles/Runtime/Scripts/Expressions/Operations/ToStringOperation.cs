using System;
using System.Globalization;
using Newtonsoft.Json;
using UnityEngine;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>to-string</c> expression operator, which coerces
    /// its single operand to a string (empty if null), formatting numbers, bools,
    /// colors, or JSON-serializing other types.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#to-string">
    ///   Mapbox “to-string” expression reference
    /// </seealso>
    public static class ToStringOperation
    {
        /// <summary>The Mapbox operator string for “to-string”.</summary>
        public const string Code = "to-string";

        /// <summary>
        /// Evaluates the <c>to-string</c> expression by converting its single operand:
        /// null ⇒ "", string ⇒ itself, bool ⇒ "true"/"false", number ⇒ invariant, 
        /// Color ⇒ "rgba(r,g,b,a)", else JSON-stringified.
        /// </summary>
        /// <param name="expression">
        ///   The <see cref="Expression"/> whose first operand is to be stringified.
        /// </param>
        /// <param name="context">
        ///   The <see cref="ExpressionContext"/> providing any nested evaluation.
        /// </param>
        /// <returns>The operand converted to <see cref="string"/>.</returns>
        /// <exception cref="InvalidOperationException">
        ///   Never thrown: all inputs have a defined fallback.
        /// </exception>
        public static string Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardNumberOfOperands(Code, expression, expected: 1);

            object rawValue = ExpressionEvaluator.Evaluate(expression, 0, context);

            // Start with the most performant cases:
            switch (rawValue)
            {
                case null: return "";
                case string str: return str;
                case bool boolVal: return boolVal.ToString().ToLowerInvariant();
                case Color color:
                {
                    int r = Mathf.RoundToInt(color.r * 255f);
                    int g = Mathf.RoundToInt(color.g * 255f);
                    int b = Mathf.RoundToInt(color.b * 255f);
                    string alpha = color.a.ToString(CultureInfo.InvariantCulture);
                    return $"rgba({r},{g},{b},{alpha})";
                }
            }

            // If it's a number, format it with invariant culture.
            if (Operations.IsNumber(rawValue))
            {
                return Operations.ToDouble(rawValue).ToString(CultureInfo.InvariantCulture);
            }

            // Fallback: JSON-serialize and strip quotes if it's a JSON string
            string json = JsonConvert.SerializeObject(rawValue, Formatting.None);
            if (json.Length >= 2 && json[0] == '"' && json[^1] == '"')
            {
                return json.Substring(1, json.Length - 2);
            }

            return json;
        }
    }
}
