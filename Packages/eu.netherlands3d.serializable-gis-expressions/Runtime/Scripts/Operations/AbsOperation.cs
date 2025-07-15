using System;
using System.Globalization;

namespace Netherlands3D.SerializableGisExpressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>abs</c> expression operator, which returns the absolute value of its numeric operand.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#abs">
    ///   Mapbox “abs” expression reference
    /// </seealso>
    public static class AbsOperation
    {
        /// <summary>The Mapbox operator string for “abs”.</summary>
        public const string Code = "abs";

        /// <summary>
        /// Evaluates the absolute‐value expression.
        /// </summary>
        /// <param name="expression">
        ///   The <see cref="Expression"/> whose first operand is expected to evaluate to a number.
        /// </param>
        /// <param name="context">
        ///   The <see cref="ExpressionContext"/> providing any feature or runtime data needed.
        /// </param>
        /// <returns>The absolute value of the numeric operand, as a <see cref="double"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the operand is not a numeric type.</exception>
        public static double Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardNumberOfOperands(Code, expression, 1);

            double number = Operations.GetOperandAsNumber(Code, "number", expression, 0, context);

            return Math.Abs(number);
        }
    }
}