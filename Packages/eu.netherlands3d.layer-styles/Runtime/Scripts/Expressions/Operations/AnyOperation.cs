namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>any</c> expression operator, which returns
    /// <c>true</c> if at least one operand evaluates to a truthy boolean.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#any">
    ///   Mapbox “any” expression reference
    /// </seealso>
    public static class AnyOperation
    {
        /// <summary>The Mapbox operator string for “any”.</summary>
        public const string Code = "any";

        /// <summary>
        /// Evaluates the <c>any</c> expression by testing each operand for truthiness.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operands are tested.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing any needed runtime data.</param>
        /// <returns><c>true</c> if any operand casts to <c>true</c>; otherwise <c>false</c>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if an operand does not evaluate to a boolean.</exception>
        public static bool Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardAtLeastNumberOfOperands(Code, expression, 1);

            var operands = expression.Operands;
            var operandCount = operands.Length;

            for (int i = 0; i < operandCount; i++)
            {
                var rawValue = ExpressionEvaluator.Evaluate(expression, i, context);

                if (Operations.AsBool(rawValue)) return true;
            }

            return false;
        }
    }
}