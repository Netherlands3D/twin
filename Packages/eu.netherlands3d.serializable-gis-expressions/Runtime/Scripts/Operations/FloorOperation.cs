using System;

namespace Netherlands3D.SerializableGisExpressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>floor</c> expression operator, which returns
    /// the largest integer less than or equal to its numeric operand.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#floor">
    ///   Mapbox “floor” expression reference
    /// </seealso>
    public static class FloorOperation
    {
        /// <summary>The Mapbox operator string for “floor”.</summary>
        public const string Code = "floor";

        /// <summary>
        /// Evaluates the <c>floor</c> expression by rounding its operand down.
        /// </summary>
        /// <param name="expression">
        ///   The <see cref="Expression"/> whose first operand is expected to evaluate to a number.
        /// </param>
        /// <param name="context">
        ///   The <see cref="ExpressionContext"/> providing any feature or runtime data needed.
        /// </param>
        /// <returns>The largest integer less than or equal to the operand, as a <see cref="double"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the operand is not a numeric type.</exception>
        public static double Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardNumberOfOperands(Code, expression, 1);

            double operandValue = Operations.GetOperandAsNumber(Code, "number", expression, 0, context);

            return Math.Floor(operandValue);
        }
    }
}