using System;
using System.Globalization;
using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.ExpressionEngine.OperatorHandlers
{
    public static class AcosOperator
    {
        public const string Code = "acos";

        public static double Evaluate(Expression expr, ExpressionContext ctx)
        {
            var o = ExpressionEvaluator.Evaluate(expr, 0, ctx);
            if (!ExpressionEvaluator.IsNumber(o))
                throw new InvalidOperationException(
                    $"\"acos\" requires a numeric operand, got {o?.GetType().Name}");
            return Math.Acos(Convert.ToDouble(o, CultureInfo.InvariantCulture));
        }
    }
}