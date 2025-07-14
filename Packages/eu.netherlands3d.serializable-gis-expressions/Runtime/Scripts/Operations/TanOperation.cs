using System;

namespace Netherlands3D.SerializableGisExpressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>tan</c> expression operator, which returns the tangent
    /// of its single numeric operand (in radians).
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#tan">
    ///   Mapbox “tan” expression reference
    /// </seealso>
    public static class TanOperation
    {
        /// <summary>The Mapbox operator string for “tan”.</summary>
        public const string Code = "tan";

        /// <summary>
        /// Evaluates the <c>tan</c> expression by parsing exactly one numeric operand
        /// and returning its tangent.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose first operand is the angle in radians.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing any runtime data.</param>
        /// <returns>The tangent of the operand, as a <see cref="double"/>.</returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if operand count is not 1 or the operand is non-numeric.
        /// </exception>
        public static double Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardNumberOfOperands(Code, expression, 1);

            double angle = Operations.GetNumericOperand(Code, "angle", expression, 0, context);

            return Math.Tan(angle);
        }
    }
}