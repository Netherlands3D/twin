using System;
using System.Globalization;
using System.Linq;
using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.ExpressionEngine.OperatorHandlers
{
    public static class MaxOperator
    {
        public const string Code = "max";

        public static double Evaluate(Expression expr, ExpressionContext ctx)
        {
            return expr.Operands
                .Select((o, i) => ExpressionEvaluator.Evaluate(expr, i, ctx))
                .Select(o =>
                {
                    if (!ExpressionEvaluator.IsNumber(o))
                        throw new InvalidOperationException(
                            $"\"max\" requires numeric operands, got {o?.GetType().Name}");
                    return Convert.ToDouble(o, CultureInfo.InvariantCulture);
                })
                .Max();
        }
    }
}