using System;
using System.Globalization;

namespace Netherlands3D.SerializableGisExpressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>+</c> expression operator, which returns the sum
    /// of all numeric operands.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#%2B">
    ///   Mapbox “+” expression reference
    /// </seealso>
    public static class AddOperation
    {
        /// <summary>The Mapbox operator string for “+”.</summary>
        public const string Code = "+";

        /// <summary>
        /// Evaluates the addition expression by summing all operands.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operands are expected to be numeric.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing any needed runtime data.</param>
        /// <returns>The sum of all operand values, as a <see cref="double"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if any operand is not a numeric type.</exception>
        public static double Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardAtLeastNumberOfOperands(Code, expression, 1);

            double sum = 0.0;
            for (int i = 0; i < expression.Operands.Length; i++)
            {
                sum += Operations.GetOperandAsNumber(Code, $"operand {i}", expression, i, context);
            }

            return sum;
        }
    }
}