namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>!</c> expression operator, which returns the logical
    /// negation of its single boolean operand.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#%21">
    ///   Mapbox “!” expression reference
    /// </seealso>
    public static class NotOperation
    {
        /// <summary>The Mapbox operator string for “!”.</summary>
        public const string Code = "!";

        /// <summary>
        /// Evaluates the <c>!</c> expression by converting its single operand to a boolean and returning its negation.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose first operand is negated.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing any feature or runtime data.</param>
        /// <returns>The negated boolean value of the operand.</returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if the operand count is not 1 or if the operand cannot be converted to boolean.
        /// </exception>
        public static bool Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardNumberOfOperands(Code, expression, expected: 1);
            
            object result = ExpressionEvaluator.Evaluate(expression, 0, context);
            
            return !Operations.AsBool(result);
        }
    }
}