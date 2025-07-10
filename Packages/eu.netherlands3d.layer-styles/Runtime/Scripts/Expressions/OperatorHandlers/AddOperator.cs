using System;
using System.Globalization;
using System.Linq;
using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.Expressions.OperatorHandlers
{
    public static class AddOperator
    {
        public const string Code = "+";

        public static double Evaluate(Expression expr, ExpressionContext ctx)
        {
            return expr.Operands
                .Select((o, i) => ExpressionEvaluator.Evaluate(expr, i, ctx))
                .Aggregate(0.0, (sum, o) =>
                {
                    if (!ExpressionEvaluator.IsNumber(o))
                        throw new InvalidOperationException(
                            $"\"+\" requires numeric operands, got {o?.GetType().Name}");
                    return sum + Convert.ToDouble(o, CultureInfo.InvariantCulture);
                });
        }
    }
}