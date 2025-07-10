namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>*</c> expression operator, which returns the
    /// product of its numeric operands.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#%2A">
    ///   Mapbox “*” expression reference
    /// </seealso>
    public static class MultiplyOperation
    {
        /// <summary>The Mapbox operator string for “*”.</summary>
        public const string Code = "*";

        /// <summary>
        /// Evaluates the <c>*</c> expression by multiplying all operands parsed as numbers.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operands yield numeric values.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing runtime data.</param>
        /// <returns>The product of the operands, as a <see cref="double"/>.</returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if any operand is not numeric.
        /// </exception>
        public static double Evaluate(Expression expression, ExpressionContext context)
        {
            double product = 1.0;
            int operandCount = expression.Operands.Length;

            for (int i = 0; i < operandCount; i++)
            {
                double value = Operations.GetNumericOperand(Code, $"operand {i}", expression, i, context);

                product *= value;
            }

            return product;
        }
    }
}