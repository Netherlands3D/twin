using System;
using System.Globalization;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>asin</c> expression operator, which returns
    /// the arcsine (in radians) of its numeric operand.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#asin">
    ///   Mapbox “asin” expression reference
    /// </seealso>
    public static class AsinOperation
    {
        /// <summary>The Mapbox operator string for “asin”.</summary>
        public const string Code = "asin";

        /// <summary>
        /// Evaluates the arc-sine expression.
        /// </summary>
        /// <param name="expression">
        ///   The <see cref="Expression"/> whose first operand is expected to evaluate to a number.
        /// </param>
        /// <param name="context">
        ///   The <see cref="ExpressionContext"/> providing any feature or runtime data needed.
        /// </param>
        /// <returns>The arcsine of the operand, in radians, as a <see cref="double"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the operand is not a numeric type.</exception>
        public static double Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardNumberOfOperands(Code, expression, 1);

            double operandValue = Operations.GetNumericOperand(Code, "number", expression, 0, context);

            return Math.Asin(operandValue);
        }
    }
}