namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>literal</c> expression operator, which returns
    /// its operands exactly as given, with no further evaluation.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#literal">
    ///   Mapbox “literal” expression reference
    /// </seealso>
    internal static class LiteralOperation
    {
        /// <summary>The Mapbox operator string for “literal”.</summary>
        public const string Code = "literal";

        /// <summary>
        /// Evaluates the <c>literal</c> expression by returning each operand
        /// without further evaluation, recursively unpacking any nested Expression.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operands are to be returned literally.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> (unused by this operator).</param>
        /// <returns>
        ///   An <c>object[]</c> containing the literal operand values, nested arrays
        ///   unpacked into <c>object[]</c> as needed.
        /// </returns>
        public static object Evaluate(Expression expression)
        {
            Operations.GuardAtLeastNumberOfOperands(Code, expression, 1);

            int count = expression.Operands.Length;
            var result = new object[count];

            for (int i = 0; i < count; i++)
            {
                object operand = expression.Operands[i];
                if (operand is Expression nested)
                {
                    result[i] = Unpack(nested);
                    continue;
                }

                result[i] = operand;
            }

            return result;
        }

        /// <summary>
        /// Recursively unpacks a nested literal <see cref="Expression"/> into a
        /// raw object or nested <c>object[]</c>.
        /// </summary>
        /// <param name="literalExpression">The <see cref="Expression"/> representing a literal array.</param>
        /// <returns>The unpacked literal: either a primitive <c>object</c> or an <c>object[]</c>.</returns>
        // TODO: Check if this ain't a duplicate of the above??
        private static object Unpack(Expression literalExpression)
        {
            int count = literalExpression.Operands.Length;
            var array = new object[count];

            for (int i = 0; i < count; i++)
            {
                object element = literalExpression.Operands[i];
                if (element is Expression nested)
                {
                    array[i] = Unpack(nested);
                    continue;
                }

                array[i] = element;
            }

            return array;
        }
    }
}
