using System;

namespace Netherlands3D.SerializableGisExpressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>sin</c> expression operator, which returns the sine
    /// of its numeric operand (in radians).
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#sin">
    ///   Mapbox “sin” expression reference
    /// </seealso>
    public static class SinOperation
    {
        /// <summary>The Mapbox operator string for “sin”.</summary>
        public const string Code = "sin";

        /// <summary>
        /// Evaluates the <c>sin</c> expression by parsing and validating its
        /// single numeric operand, then computing its sine.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operand is the angle in radians.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing any feature or runtime data.</param>
        /// <returns>The sine of the input value.</returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if operand count is not 1 or the operand is not numeric.
        /// </exception>
        public static double Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardNumberOfOperands(Code, expression, 1);
            
            double value = Operations.GetOperandAsNumber(Code, "value", expression, 0, context);
            
            return Math.Sin(value);
        }
    }
}