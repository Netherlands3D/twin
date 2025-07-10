using System;
using System.Linq;
using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.Expressions.OperatorHandlers
{
    /// <summary>
    /// ["string", v₀, fallback₁, …] → first operand that is a string; error otherwise.
    /// </summary>
    internal static class StringOperator
    {
        public const string Code = "string";

        public static string Evaluate(Expression expr, ExpressionContext ctx)
        {
            foreach (var (_, idx) in expr.Operands.Select((o, i) => (o, i)))
            {
                var val = ExpressionEvaluator.Evaluate(expr, idx, ctx);
                if (val is string s) return s;
            }

            throw new InvalidOperationException(
                $"\"string\" assertion failed: no operand evaluated to a string."
            );
        }
    }
}