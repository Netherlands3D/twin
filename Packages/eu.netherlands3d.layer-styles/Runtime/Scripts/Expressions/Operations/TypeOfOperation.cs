using UnityEngine;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>typeof</c> expression operator, which returns the
    /// runtime type of its single operand as one of: "number", "boolean", "string",
    /// "color", "array", "null", or "object".
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#typeof">
    ///   Mapbox “typeof” expression reference
    /// </seealso>
    public static class TypeOfOperation
    {
        /// <summary>The Mapbox operator string for “typeof”.</summary>
        public const string Code = "typeof";

        /// <summary>
        /// Evaluates the <c>typeof</c> expression by determining the type of the first operand.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose first operand is to be inspected.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing any nested evaluation.</param>
        /// <returns>
        ///   A <see cref="string"/> naming the type: "number", "boolean", "string",
        ///   "color", "array", "null", or "object".
        /// </returns>
        public static string Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardNumberOfOperands(Code, expression, 1);

            object rawValue = ExpressionEvaluator.Evaluate(expression, 0, context);

            // First the performant cases, then the less performant cases.
            var result = rawValue switch
            {
                bool => "boolean",
                string => "string",
                Color => "color",
                object[] => "array",
                null => "null",
                _ => null
            };

            if (result == null && ExpressionEvaluator.IsNumber(rawValue))
            {
                result = "number";
            }

            return result ?? "object";
        }
    }
}