using System;

namespace Netherlands3D.SerializableGisExpressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>^</c> expression operator, which raises its
    /// first numeric operand (base) to the power of its second numeric operand (exponent).
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#%5E">
    ///   Mapbox “^” expression reference
    /// </seealso>
    internal static class PowerOperation
    {
        /// <summary>The Mapbox operator code for “^”.</summary>
        public const string Code = "^";

        /// <summary>
        /// Evaluates the <c>^</c> expression by parsing and validating exactly
        /// two numeric operands, then computing <c>Math.Pow(base, exponent)</c>.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operands are [base, exponent].</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing any runtime data.</param>
        /// <returns>The result of raising <c>base</c> to <c>exponent</c>, as a <see cref="double"/>.</returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if operand count is not 2 or any operand is not numeric.
        /// </exception>
        public static double Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardNumberOfOperands(Code, expression, expected: 2);

            double baseValue = Operations.GetNumericOperand(Code, "base", expression, 0, context);
            double exponentValue = Operations.GetNumericOperand(Code, "exponent", expression, 1, context);

            return Math.Pow(baseValue, exponentValue);
        }
    }
}