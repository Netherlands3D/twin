using System;

namespace Netherlands3D.SerializableGisExpressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>number</c> expression operator, which returns the first operand that already evaluates
    /// to a numeric value, or throws.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#types-number">
    ///   Mapbox “number” expression reference
    /// </seealso>
    internal static class NumberOperation
    {
        /// <summary>The Mapbox operator code for “number”.</summary>
        public const string Code = "number";

        /// <summary>
        /// Evaluates the <c>number</c> expression by returning the first operand that evaluates to a number. Throws
        /// if none is numeric.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operands will be tested in order.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing runtime feature data.</param>
        /// <returns>The first numeric operand as a <see cref="double"/>.</returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if no operand evaluates to a numeric value.
        /// </exception>
        public static double Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardAtLeastNumberOfOperands(Code, expression, atLeast: 1);

            for (int i = 0; i < expression.Operands.Length; i++)
            {
                object value = ExpressionEvaluator.Evaluate(expression, i, context);
                if (!Operations.IsNumber(value)) continue;

                return Operations.ToDouble(value);
            }

            throw new InvalidOperationException($"\"{Code}\" assertion failed: no operand evaluated to a number.");
        }
    }
}
