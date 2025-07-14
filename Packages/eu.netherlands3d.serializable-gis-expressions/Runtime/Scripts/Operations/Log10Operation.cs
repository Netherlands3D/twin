using System;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>log10</c> expression operator, which returns the
    /// base-10 logarithm of its single numeric operand.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#log10">
    ///   Mapbox “log10” expression reference
    /// </seealso>
    public static class Log10Operation
    {
        /// <summary>The Mapbox operator string for “log10”.</summary>
        public const string Code = "log10";

        /// <summary>
        /// Evaluates the <c>log10</c> expression by computing the base-10 logarithm of its sole operand.
        /// </summary>
        /// <param name="expression">
        ///   The <see cref="Expression"/> whose single operand is the number to take the base-10 logarithm of.
        /// </param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing runtime data (unused here).</param>
        /// <returns>The base-10 logarithm of the operand, as a <see cref="double"/>.</returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if operand count is not 1 or the operand is not numeric.
        /// </exception>
        public static double Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardNumberOfOperands(Code, expression, expected: 1);
            
            double value = Operations.GetNumericOperand(Code, "value", expression, index: 0, context);

            return Math.Log10(value);
        }
    }
}