using System;
using System.Globalization;
using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.Expressions.OperatorHandlers
{
    public static class AbsOperator
    {
        public const string Code = "abs";

        public static double Evaluate(Expression expr, ExpressionContext ctx)
        {
            var o = ExpressionEvaluator.Evaluate(expr, 0, ctx);
            if (!ExpressionEvaluator.IsNumber(o))
                throw new InvalidOperationException($"\"abs\" requires a numeric operand, got {o?.GetType().Name}");
            return Math.Abs(Convert.ToDouble(o, CultureInfo.InvariantCulture));
        }
    }
}