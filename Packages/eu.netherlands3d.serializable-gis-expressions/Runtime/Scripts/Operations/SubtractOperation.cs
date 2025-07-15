using System;

namespace Netherlands3D.SerializableGisExpressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>-</c> expression operator, which subtracts its operands.
    /// If a single operand is provided, returns its negation; otherwise folds subtraction
    /// across all operands.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#-">
    ///   Mapbox “-” expression reference
    /// </seealso>
    public static class SubtractOperation
    {
        /// <summary>The Mapbox operator string for “-”.</summary>
        public const string Code = "-";

        /// <summary>
        /// Evaluates the <c>-</c> expression by parsing at least one numeric operand,
        /// negating it if alone, or subtracting each subsequent operand from the first.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operands supply the values to subtract.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing any runtime data.</param>
        /// <returns>The subtraction result as a <see cref="double"/>.</returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if fewer than one operand is provided or any operand is non-numeric.
        /// </exception>
        public static double Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardAtLeastNumberOfOperands(Code, expression, 1);

            // Get the minuend (or the value to negate if only one operand)
            double result = Operations.GetOperandAsNumber(Code, "operand 0", expression, 0, context);

            // Single operand → unary negation
            if (expression.Operands.Length == 1)
            {
                return -result;
            }

            // Fold subtraction: result minus each subtrahend
            for (int i = 1; i < expression.Operands.Length; i++)
            {
                double value = Operations.GetOperandAsNumber(Code, $"operand {i}", expression, i, context);
                result -= value;
            }

            return result;
        }
    }
}
