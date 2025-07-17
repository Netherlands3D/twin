namespace Netherlands3D.SerializableGisExpressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>==</c> expression operator, which performs
    /// strict equality comparison between two operands.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#==">
    ///   Mapbox “==” expression reference
    /// </seealso>
    public static class EqualOperation
    {
        /// <summary>The Mapbox operator string for “==”.</summary>
        public const string Code = "==";

        /// <summary>
        /// Evaluates the equality expression by comparing the first two operands.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operands to compare.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing any feature or runtime data.</param>
        /// <returns><c>true</c> if both operand values are equal; otherwise <c>false</c>.</returns>
        public static bool Evaluate(Expression expression, ExpressionContext context)
        {
            var leftValue = ExpressionEvaluator.Evaluate(expression, 0, context);
            var rightValue = ExpressionEvaluator.Evaluate(expression, 1, context);

            return Operations.IsEqual(leftValue, rightValue);
        }
    }
}