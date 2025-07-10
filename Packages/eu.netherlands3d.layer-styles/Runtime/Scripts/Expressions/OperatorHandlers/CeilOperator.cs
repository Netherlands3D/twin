using System;
using System.Globalization;
using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.Expressions.OperatorHandlers
{
    public static class CeilOperator
    {
        public const string Code = "ceil";

        public static double Evaluate(Expression expr, ExpressionContext ctx)
        {
            var o = ExpressionEvaluator.Evaluate(expr, 0, ctx);
            if (!ExpressionEvaluator.IsNumber(o))
                throw new InvalidOperationException(
                    $"\"ceil\" requires a numeric operand, got {o?.GetType().Name}");
            return Math.Ceiling(Convert.ToDouble(o, CultureInfo.InvariantCulture));
        }
    }
}