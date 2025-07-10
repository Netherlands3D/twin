using System;
using System.Globalization;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    public static class RoundOperation
    {
        public const string Code = "round";

        public static double Evaluate(Expression expr, ExpressionContext ctx)
        {
            var o = ExpressionEvaluator.Evaluate(expr, 0, ctx);
            if (!ExpressionEvaluator.IsNumber(o))
                throw new InvalidOperationException(
                    $"\"round\" requires a numeric operand, got {o?.GetType().Name}");
            return Math.Round(Convert.ToDouble(o, CultureInfo.InvariantCulture));
        }
    }
}