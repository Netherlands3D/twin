using System;

namespace Netherlands3D.SerializableGisExpressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>%</c> expression operator, which returns the remainder
    /// of dividing the first operand by each subsequent operand in sequence.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#%25">
    ///   Mapbox “%” expression reference
    /// </seealso>
    public static class ModuloOperation
    {
        /// <summary>The Mapbox operator string for “%”.</summary>
        public const string Code = "%";

        /// <summary>
        /// Evaluates the <c>%</c> expression by parsing each operand as a number,
        /// then folding remainder operations left-to-right.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operands yield numeric values.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing runtime data.</param>
        /// <returns>The result of the chained modulo operations, as a <see cref="double"/>.</returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if fewer than two operands are provided or any operand is not numeric.
        /// </exception>
        public static double Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardAtLeastNumberOfOperands(Code, expression, 2);

            double result = Operations.GetNumericOperand(Code, "operand0", expression, 0, context);
            
            for (int i = 1; i < expression.Operands.Length; i++)
            {
                double divisor = Operations.GetNumericOperand(Code, $"operand{i}", expression, i, context);

                result %= divisor;
            }
            
            return result;
        }
    }
}
