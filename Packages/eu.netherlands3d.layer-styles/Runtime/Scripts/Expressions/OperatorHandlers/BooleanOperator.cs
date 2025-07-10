using System;
using System.Linq;
using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.Expressions.OperatorHandlers
{
    public static class BooleanOperator
    {
        public const string Code = "boolean";

        public static bool Evaluate(Expression expression, ExpressionContext context)
        {
            foreach (var (_, idx) in expression.Operands.Select((o, i) => (o, i)))
            {
                var o = ExpressionEvaluator.Evaluate(expression, idx, context);
                if (o is bool b) return b;
            }

            throw new InvalidOperationException($"\"boolean\" assertion failed: no operand evaluated to boolean.");
        }
    }
}