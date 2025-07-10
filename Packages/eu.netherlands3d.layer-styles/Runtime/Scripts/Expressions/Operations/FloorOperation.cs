using System;
using System.Globalization;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>floor</c> expression operator, which returns
    /// the largest integer less than or equal to its numeric operand.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#floor">
    ///   Mapbox “floor” expression reference
    /// </seealso>
    public static class FloorOperation
    {
        /// <summary>The Mapbox operator string for “floor”.</summary>
        public const string Code = "floor";

        /// <summary>
        /// Evaluates the <c>floor</c> expression by rounding its operand down.
        /// </summary>
        /// <param name="expression">
        ///   The <see cref="Expression"/> whose first operand is expected to evaluate to a number.
        /// </param>
        /// <param name="context">
        ///   The <see cref="ExpressionContext"/> providing any feature or runtime data needed.
        /// </param>
        /// <returns>The largest integer less than or equal to the operand, as a <see cref="double"/>.</returns>
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

            return Math.Floor(Convert.ToDouble(operandValue, CultureInfo.InvariantCulture));
        }
    }
}