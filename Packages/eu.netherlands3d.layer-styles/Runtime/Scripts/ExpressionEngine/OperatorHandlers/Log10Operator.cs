using System;
using System.Globalization;
using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.ExpressionEngine.OperatorHandlers
{
    public static class Log10Operator
    {
        public const string Code = "log10";

        public static double Evaluate(Expression expr, ExpressionContext ctx)
        {
            var o = ExpressionEvaluator.Evaluate(expr, 0, ctx);
            if (!ExpressionEvaluator.IsNumber(o))
                throw new InvalidOperationException(
                    $"\"log10\" requires a numeric operand, got {o?.GetType().Name}");
            return Math.Log10(Convert.ToDouble(o, CultureInfo.InvariantCulture));
        }
    }
}