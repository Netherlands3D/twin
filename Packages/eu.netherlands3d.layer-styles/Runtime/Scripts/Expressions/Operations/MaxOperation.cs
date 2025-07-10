using System;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>max</c> expression operator, which returns the
    /// greatest numeric value among its operands.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#max">
    ///   Mapbox “max” expression reference
    /// </seealso>
    public static class MaxOperation
    {
        /// <summary>The Mapbox operator string for “max”.</summary>
        public const string Code = "max";

        /// <summary>
        /// Evaluates the <c>max</c> expression by parsing each operand as a number and returning the maximum.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operands yield numeric values.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing runtime data.</param>
        /// <returns>The maximum operand value, as a <see cref="double"/>.</returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if there are no operands or any operand is not numeric.
        /// </exception>
        public static double Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardAtLeastNumberOfOperands(Code, expression, 1);

            double maxValue = Operations.GetNumericOperand(Code, "operand0", expression, 0, context);

            for (int i = 1; i < expression.Operands.Length; i++)
            {
                double candidate = Operations.GetNumericOperand(Code, $"operand{i}", expression, i, context);

                if (candidate > maxValue)
                {
                    maxValue = candidate;
                }
            }

            return maxValue;
        }
    }
}
