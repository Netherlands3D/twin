using System;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>&gt;=</c> expression operator, which returns
    /// true if the first operand is numerically greater than or equal to the second.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#>=">
    ///   Mapbox “&gt;=” expression reference
    /// </seealso>
    public static class GreaterThanOrEqualOperation
    {
        /// <summary>The Mapbox operator string for “&gt;=”.</summary>
        public const string Code = ">=";

        /// <summary>
        /// Evaluates the greater-than-or-equal expression by comparing the first two operands.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose first two operands are compared.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing any feature or runtime data.</param>
        /// <returns><c>true</c> if <c>left &gt;= right</c>; otherwise <c>false</c>.</returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if the operands cannot be compared (e.g., non-numeric types).
        /// </exception>
        public static bool Evaluate(Expression expression, ExpressionContext context)
        {
            var leftValue  = ExpressionEvaluator.Evaluate(expression, 0, context);
            var rightValue = ExpressionEvaluator.Evaluate(expression, 1, context);

            return ExpressionEvaluator.Compare(leftValue, rightValue) >= 0;
        }
    }
}