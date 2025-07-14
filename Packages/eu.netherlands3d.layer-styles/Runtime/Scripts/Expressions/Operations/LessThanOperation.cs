using System;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>&lt;</c> expression operator, which returns
    /// <c>true</c> if the first operand is strictly less than the second.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#%3C">
    ///   Mapbox “&lt;” expression reference
    /// </seealso>
    public static class LessThanOperation
    {
        /// <summary>The Mapbox operator string for “&lt;”.</summary>
        public const string Code = "<";

        /// <summary>
        /// Evaluates the <c>&lt;</c> expression by comparing its two operands.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operands are the two values to compare.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing any runtime data.</param>
        /// <returns><c>true</c> if the first operand is less than the second; otherwise <c>false</c>.</returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if the operand count is not 2 or if the operands are not comparable
        ///   numeric or string types.
        /// </exception>
        public static bool Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardNumberOfOperands(Code, expression, expected: 2);

            object left = ExpressionEvaluator.Evaluate(expression, 0, context);
            object right = ExpressionEvaluator.Evaluate(expression, 1, context);

            return Operations.Compare(left, right) < 0;
        }
    }
}