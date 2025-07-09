using System;
using System.Globalization;
using System.Linq;
using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.ExpressionEngine.OperatorHandlers
{
    public static class MultiplyOperator
    {
        public const string Code = "*";

        public static double Evaluate(Expression expr, ExpressionContext ctx)
        {
            return expr.Operands
                .Select((o, i) => ExpressionEvaluator.Evaluate(expr, i, ctx))
                .Aggregate(1.0, (prod, o) =>
                {
                    if (!ExpressionEvaluator.IsNumber(o))
                        throw new InvalidOperationException(
                            $"\"*\" requires numeric operands, got {o?.GetType().Name}");
                    return prod * Convert.ToDouble(o, CultureInfo.InvariantCulture);
                });
        }
    }
}