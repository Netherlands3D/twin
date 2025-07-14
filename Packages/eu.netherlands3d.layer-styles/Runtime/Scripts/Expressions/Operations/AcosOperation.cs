using System;
using System.Globalization;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>acos</c> expression operator, which returns the arccosine (in radians) of
    /// its numeric operand.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#acos">
    ///   Mapbox “acos” expression reference
    /// </seealso>
    public static class AcosOperation
    {
        /// <summary>The Mapbox operator string for “acos”.</summary>
        public const string Code = "acos";

        /// <summary>
        /// Evaluates the arc-cosine expression.
        /// </summary>
        /// <param name="expression">
        ///   The <see cref="Expression"/> whose first operand is expected to evaluate to a number.
        /// </param>
        /// <param name="context">
        ///   The <see cref="ExpressionContext"/> providing any feature or runtime data needed.
        /// </param>
        /// <returns>The arccosine of the operand, in radians, as a <see cref="double"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the operand is not a numeric type.</exception>
        public static double Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardNumberOfOperands(Code, expression, 1);

            double number = Operations.GetNumericOperand(Code, "number", expression, 0, context);

            return Math.Acos(number);
        }
    }
}