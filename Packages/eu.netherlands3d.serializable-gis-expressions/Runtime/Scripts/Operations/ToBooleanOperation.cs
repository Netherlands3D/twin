using System;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>to-boolean</c> expression operator, which casts its
    /// single operand to a boolean using Mapbox truthiness rules:
    /// null, 0, NaN, empty string, or empty array → false; otherwise true.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#to-boolean">
    ///   Mapbox “to-boolean” expression reference
    /// </seealso>
    public static class ToBooleanOperation
    {
        /// <summary>The Mapbox operator string for “to-boolean”.</summary>
        public const string Code = "to-boolean";

        /// <summary>
        /// Evaluates the <c>to-boolean</c> expression by casting its single operand
        /// to a <see cref="bool"/> according to Mapbox truthiness.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose first operand is the value to cast.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing any runtime data.</param>
        /// <returns>
        ///   <c>false</c> for null, zero, NaN, empty string, or empty array;
        ///   otherwise <c>true</c>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if the operand count is not exactly one.
        /// </exception>
        public static bool Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardNumberOfOperands(Code, expression, 1);

            object value = ExpressionEvaluator.Evaluate(expression, 0, context);

            if (Operations.IsNumber(value))
            {
                double d = Operations.ToDouble(value);

                return d is not (0 or double.NaN);
            }

            return value switch
            {
                null => false,
                bool boolean => boolean,
                string str => str.Length > 0,
                object[] arr => arr.Length > 0,
                _ => true // Any other non-null value is truthy
            };
        }
    }
}