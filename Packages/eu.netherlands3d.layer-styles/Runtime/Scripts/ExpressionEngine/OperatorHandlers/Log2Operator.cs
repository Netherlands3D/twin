using System;
using System.Globalization;
using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.ExpressionEngine.OperatorHandlers
{
    public static class Log2Operator
    {
        public const string Code = "log2";

        public static double Evaluate(Expression expr, ExpressionContext ctx)
        {
            var o = ExpressionEvaluator.Evaluate(expr, 0, ctx);
            if (!ExpressionEvaluator.IsNumber(o))
                throw new InvalidOperationException(
                    $"\"log2\" requires a numeric operand, got {o?.GetType().Name}");
            return Math.Log(Convert.ToDouble(o, CultureInfo.InvariantCulture), 2.0);
        }
    }
}