using System;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>string</c> expression operator, which returns the
    /// first operand that evaluates to a string.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#string">
    ///   Mapbox “string” expression reference
    /// </seealso>
    internal static class StringOperation
    {
        /// <summary>The Mapbox operator string for “string”.</summary>
        public const string Code = "string";

        /// <summary>
        /// Evaluates the <c>string</c> expression by scanning its operands in order
        /// and returning the first one that is a string.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operands will be tested.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing any runtime data.</param>
        /// <returns>The first operand value of type <see cref="string"/>.</returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if there are no operands or none evaluate to a string.
        /// </exception>
        public static string Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardAtLeastNumberOfOperands(Code, expression, 1);

            for (int i = 0; i < expression.Operands.Length; i++)
            {
                object raw = ExpressionEvaluator.Evaluate(expression, i, context);
                
                if (raw is string s) return s;
            }

            throw new InvalidOperationException($"\"{Code}\" assertion failed: no operand evaluated to a string.");
        }
    }
}