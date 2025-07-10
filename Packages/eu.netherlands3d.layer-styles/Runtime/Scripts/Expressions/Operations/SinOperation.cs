using System;
using System.Globalization;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    public static class SinOperation
    {
        public const string Code = "sin";

        public static double Evaluate(Expression expr, ExpressionContext ctx)
        {
            var o = ExpressionEvaluator.Evaluate(expr, 0, ctx);
            if (!ExpressionEvaluator.IsNumber(o))
                throw new InvalidOperationException($"\"sin\" requires a numeric operand, got {o?.GetType().Name}");
            return Math.Sin(Convert.ToDouble(o, CultureInfo.InvariantCulture));
        }
    }
}