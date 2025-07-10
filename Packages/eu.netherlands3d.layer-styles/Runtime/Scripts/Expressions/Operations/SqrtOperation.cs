using System;
using System.Globalization;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    public static class SqrtOperation
    {
        public const string Code = "sqrt";

        public static double Evaluate(Expression expr, ExpressionContext ctx)
        {
            var o = ExpressionEvaluator.Evaluate(expr, 0, ctx);
            if (!ExpressionEvaluator.IsNumber(o))
                throw new InvalidOperationException(
                    $"\"sqrt\" requires a numeric operand, got {o?.GetType().Name}");
            return Math.Sqrt(Convert.ToDouble(o, CultureInfo.InvariantCulture));
        }
    }
}