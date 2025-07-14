namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>all</c> expression operator, which returns
    /// <c>true</c> if every operand evaluates to a truthy boolean.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#all">
    ///   Mapbox “all” expression reference
    /// </seealso>
    public static class AllOperation
    {
        /// <summary>The Mapbox operator string for “all”.</summary>
        public const string Code = "all";

        /// <summary>
        /// Evaluates the <c>all</c> expression by ensuring every operand is truthy.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operands are tested.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing any feature or runtime data.</param>
        /// <returns><c>true</c> if all operands cast to <c>true</c>; otherwise <c>false</c>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if any operand does not evaluate to a boolean.</exception>
        public static bool Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardAtLeastNumberOfOperands(Code, expression, 1);
            
            var operands = expression.Operands;
            var operandCount = operands.Length;

            for (int i = 0; i < operandCount; i++)
            {
                var rawValue = ExpressionEvaluator.Evaluate(expression, i, context);

                if (!Operations.AsBool(rawValue)) return false;
            }

            return true;
        }
    }
}