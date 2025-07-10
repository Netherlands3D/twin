using System;
using System.Globalization;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>cos</c> expression operator, which returns
    /// the cosine (in radians) of its numeric operand.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#cos">
    ///   Mapbox “cos” expression reference
    /// </seealso>
    public static class CosOperation
    {
        /// <summary>The Mapbox operator string for “cos”.</summary>
        public const string Code = "cos";

        /// <summary>
        /// Evaluates the cosine expression.
        /// </summary>
        /// <param name="expression">
        ///   The <see cref="Expression"/> whose first operand is expected to be numeric.
        /// </param>
        /// <param name="context">
        ///   The <see cref="ExpressionContext"/> providing any feature or runtime data needed.
        /// </param>
        /// <returns>The cosine of the operand, in radians, as a <see cref="double"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the operand is not a numeric type.</exception>
        public static double Evaluate(Expression expression, ExpressionContext context)
        {
            var operandValue = ExpressionEvaluator.Evaluate(expression, 0, context);

            if (!ExpressionEvaluator.IsNumber(operandValue))
            {
                throw new InvalidOperationException(
                    $"\"{Code}\" requires a numeric operand, got {operandValue?.GetType().Name}"
                );
            }

            return Math.Cos(Convert.ToDouble(operandValue, CultureInfo.InvariantCulture));
        }
    }
}