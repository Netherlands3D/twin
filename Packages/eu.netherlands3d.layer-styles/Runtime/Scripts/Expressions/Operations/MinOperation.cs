using System;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>min</c> expression operator, which returns the
    /// smallest numeric value among its operands.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#min">
    ///   Mapbox “min” expression reference
    /// </seealso>
    public static class MinOperation
    {
        /// <summary>The Mapbox operator string for “min”.</summary>
        public const string Code = "min";

        /// <summary>
        /// Evaluates the <c>min</c> expression by parsing each operand as a number and returning the minimum.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operands yield numeric values.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing runtime data.</param>
        /// <returns>The minimum operand value, as a <see cref="double"/>.</returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if there are no operands or any operand is not numeric.
        /// </exception>
        public static double Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardAtLeastNumberOfOperands(Code, expression, 1);

            double minValue = Operations.GetNumericOperand(Code, "operand 0", expression, 0, context);

            for (int i = 1; i < expression.Operands.Length; i++)
            {
                double candidate = Operations.GetNumericOperand(Code, $"operand {i}", expression, i, context);

                if (candidate < minValue) minValue = candidate;
            }

            return minValue;
        }
    }
}
