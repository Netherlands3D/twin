using System;
using System.Globalization;
using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.ExpressionEngine.OperatorHandlers
{
    public static class PowerOperator
    {
        public const string Code = "^";

        public static double Evaluate(Expression expr, ExpressionContext ctx)
        {
            var ops = expr.Operands;
            if (ops.Length != 2)
                throw new InvalidOperationException("\"^\" requires exactly two operands.");

            var @base = ExpressionEvaluator.Evaluate(expr, 0, ctx);
            var exp = ExpressionEvaluator.Evaluate(expr, 1, ctx);
            if (!ExpressionEvaluator.IsNumber(@base) || !ExpressionEvaluator.IsNumber(exp))
                throw new InvalidOperationException(
                    $"\"^\" requires numeric operands, got {@base?.GetType().Name} and {exp?.GetType().Name}");

            return Math.Pow(
                Convert.ToDouble(@base, CultureInfo.InvariantCulture),
                Convert.ToDouble(exp, CultureInfo.InvariantCulture)
            );
        }
    }
}