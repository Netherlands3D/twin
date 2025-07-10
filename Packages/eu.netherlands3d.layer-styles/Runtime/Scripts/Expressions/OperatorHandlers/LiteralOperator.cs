using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.Expressions.OperatorHandlers
{
    /// <summary>
    /// ["literal", v₀, v₁, …] → returns the literal values as‐is, without further evaluation.
    /// </summary>
    internal static class LiteralOperator
    {
        public const string Code = "literal";

        public static object Evaluate(Expression expression, ExpressionContext context)
        {
            // Wrap all operands as literal values. If any operand is itself an Expression
            // (due to how the converter works), we unpack its Operands recursively.
            var operands = expression.Operands;
            var result = new object[operands.Length];
            for (int i = 0; i < operands.Length; i++)
            {
                result[i] = operands[i] is Expression nested
                    ? UnpackLiteral(nested)
                    : operands[i];
            }

            return result;
        }

        private static object UnpackLiteral(Expression literalExpr)
        {
            // The converter turns a JSON‐array literal into an Expression whose
            // .Operands are the raw items. We reconstruct the same shape.
            var inner = literalExpr.Operands;
            var arr = new object[inner.Length];
            for (int i = 0; i < inner.Length; i++)
            {
                arr[i] = inner[i] is Expression nested
                    ? UnpackLiteral(nested)
                    : inner[i];
            }

            return arr;
        }
    }
}