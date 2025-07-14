using System;
using System.Globalization;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>/</c> expression operator, which divides its first numeric
    /// operand by each subsequent numeric operand in turn.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#%2F">
    ///   Mapbox “/” expression reference
    /// </seealso>
    public static class DivideOperation
    {
        /// <summary>The Mapbox operator string for “/”.</summary>
        public const string Code = "/";

        /// <summary>
        /// Evaluates the division expression by folding each operand into the result.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operands are expected to be numeric.</param>
        /// <param name="context">
        ///   The <see cref="ExpressionContext"/> providing any feature or runtime data needed.
        /// </param>
        /// <returns>
        ///   The result of dividing the first operand by each subsequent operand, as a <see cref="double"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if fewer than two operands are provided or any operand is not numeric.
        /// </exception>
        public static double Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardAtLeastNumberOfOperands(Code, expression, 2);

            // Evaluate first operand
            double result = Operations.GetNumericOperand(Code, "operand 0", expression, 0, context);

            // Sequentially divide by each subsequent operand
            for (int i = 1; i < expression.Operands.Length; i++)
            {
                result /= Operations.GetNumericOperand(Code, $"operand {i}", expression, i, context);
            }

            return result;
        }
    }
}


