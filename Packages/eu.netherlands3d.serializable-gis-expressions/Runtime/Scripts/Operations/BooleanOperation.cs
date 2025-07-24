using System;

namespace Netherlands3D.SerializableGisExpressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>boolean</c> expression operator, which returns
    /// the first operand that evaluates to a boolean.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#types-boolean">
    ///   Mapbox “boolean” expression reference
    /// </seealso>
    public static class BooleanOperation
    {
        /// <summary>The Mapbox operator string for “boolean”.</summary>
        public const string Code = "boolean";

        /// <summary>
        /// Evaluates the <c>boolean</c> expression by finding the first boolean
        /// operand and returning its value.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operands are tested.</param>
        /// <param name="context">
        ///   The <see cref="ExpressionContext"/> providing any feature or runtime data.
        /// </param>
        /// <returns>The first operand value that is a <c>bool</c>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no operand evaluates to a boolean.</exception>
        public static bool Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardAtLeastNumberOfOperands(Code, expression, 1);

            var operands = expression.Operands;
            var operandCount = operands.Length;

            for (int i = 0; i < operandCount; i++)
            {
                var operandValue = ExpressionEvaluator.Evaluate(expression, i, context);
                
                if (operandValue is bool b) return b;
            }

            throw new InvalidOperationException($"\"{Code}\" assertion failed: no operand evaluated to boolean.");
        }
    }
}