using System;

namespace Netherlands3D.SerializableGisExpressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>sqrt</c> expression operator, which returns the
    /// square root of its single numeric operand.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#sqrt">
    ///   Mapbox “sqrt” expression reference
    /// </seealso>
    public static class SqrtOperation
    {
        /// <summary>The Mapbox operator string for “sqrt”.</summary>
        public const string Code = "sqrt";

        /// <summary>
        /// Evaluates the <c>sqrt</c> expression by parsing and validating its
        /// single numeric operand, then computing its square root.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operand is the value to root.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing any feature or runtime data.</param>
        /// <returns>The square root of the input value.</returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if operand count is not 1 or the operand is not numeric.
        /// </exception>
        public static double Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardNumberOfOperands(Code, expression, 1);
            
            double value = Operations.GetOperandAsNumber(Code, "value", expression, 0, context);
            
            return Math.Sqrt(value);
        }
    }
}