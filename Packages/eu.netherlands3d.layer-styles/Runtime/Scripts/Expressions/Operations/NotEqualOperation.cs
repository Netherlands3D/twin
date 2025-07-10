namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>!=</c> expression operator, which returns 
    /// <c>true</c> if its two operands are not equal.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#%21%3D">
    ///   Mapbox “!=” expression reference
    /// </seealso>
    public static class NotEqualOperation
    {
        /// <summary>The Mapbox operator string for “!=”.</summary>
        public const string Code = "!=";

        /// <summary>
        /// Evaluates the <c>!=</c> expression by comparing its two operands and returning the logical negation of
        /// their equality.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose first two operands are compared.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing any runtime data.</param>
        /// <returns><c>true</c> if the operands are not equal; otherwise <c>false</c>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the operand count is not 2.</exception>
        public static bool Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardNumberOfOperands(Code, expression, expected: 2);

            return !EqualOperation.Evaluate(expression, context);
        }
    }
}