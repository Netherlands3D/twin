using System;
using System.Linq;
using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.ExpressionEngine.OperatorHandlers
{
    /// <summary>
    /// ["object", v₀, fallback₁, …] → first operand that is an object; error otherwise.
    /// </summary>
    internal static class ObjectOperator
    {
        public const string Code = "object";

        public static object Evaluate(Expression expr, ExpressionContext ctx)
        {
            foreach (var (_, idx) in expr.Operands.Select((o, i) => (o, i)))
            {
                var val = ExpressionEvaluator.Evaluate(expr, idx, ctx);
                // depending on converter, this might be a JObject or a CLR IDictionary
                if (val is Newtonsoft.Json.Linq.JObject
                    || val is System.Collections.IDictionary)
                {
                    return val;
                }
            }

            throw new InvalidOperationException(
                $"\"object\" assertion failed: no operand evaluated to an object."
            );
        }
    }
}