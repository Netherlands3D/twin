using System;

namespace Netherlands3D.SerializableGisExpressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>cos</c> expression operator, which returns
    /// the cosine (in radians) of its numeric operand.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#cos">
    ///   Mapbox “cos” expression reference
    /// </seealso>
    public static class CosOperation
    {
        /// <summary>The Mapbox operator string for “cos”.</summary>
        public const string Code = "cos";

        /// <summary>
        /// Evaluates the cosine expression.
        /// </summary>
        /// <param name="expression">
        ///   The <see cref="Expression"/> whose first operand is expected to be numeric.
        /// </param>
        /// <param name="context">
        ///   The <see cref="ExpressionContext"/> providing any feature or runtime data needed.
        /// </param>
        /// <returns>The cosine of the operand, in radians, as a <see cref="double"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the operand is not a numeric type.</exception>
        public static double Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardNumberOfOperands(Code, expression, 1);

            double operandValue = Operations.GetOperandAsNumber(Code, "number", expression, 0, context);

            return Math.Cos(operandValue);
        }
    }
}