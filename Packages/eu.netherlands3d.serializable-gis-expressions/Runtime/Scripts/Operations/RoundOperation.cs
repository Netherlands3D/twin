using System;

namespace Netherlands3D.SerializableGisExpressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>round</c> expression operator, which rounds its
    /// numeric operand to the nearest integer.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#round">
    ///   Mapbox “round” expression reference
    /// </seealso>
    public static class RoundOperation
    {
        /// <summary>The Mapbox operator string for “round”.</summary>
        public const string Code = "round";

        /// <summary>
        /// Evaluates the <c>round</c> expression by parsing and validating its
        /// single numeric operand, then rounding to the nearest integer.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operand is the value to round.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing any feature or runtime data.</param>
        /// <returns>The rounded <see cref="double"/> value.</returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if operand count is not 1 or the operand is not numeric.
        /// </exception>
        public static double Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardNumberOfOperands(Code, expression, expected: 1);

            double input = Operations.GetOperandAsNumber(Code, "value", expression, 0, context);

            return Math.Round(input);
        }
    }
}