using System;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>ln</c> expression operator, which returns the
    /// natural logarithm of its single numeric operand.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#ln">
    ///   Mapbox “ln” expression reference
    /// </seealso>
    public static class LnOperation
    {
        /// <summary>The Mapbox operator string for “ln”.</summary>
        public const string Code = "ln";

        /// <summary>
        /// Evaluates the <c>ln</c> expression by computing the natural logarithm of its sole operand.
        /// </summary>
        /// <param name="expression">
        ///   The <see cref="Expression"/> whose single operand is the number to take the natural logarithm of.
        /// </param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing runtime data (unused here).</param>
        /// <returns>The natural logarithm of the operand, as a <see cref="double"/>.</returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if operand count is not 1 or the operand is not numeric.
        /// </exception>
        public static double Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardNumberOfOperands(Code, expression, expected: 1);

            double value = Operations.GetNumericOperand(Code, "value", expression, index: 0, context);

            return Math.Log(value);
        }
    }
}