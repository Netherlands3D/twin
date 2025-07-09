using System;
using System.Globalization;
using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.ExpressionEngine.OperatorHandlers
{
    public static class ModuloOperator
    {
        public const string Code = "%";

        public static double Evaluate(Expression expr, ExpressionContext ctx)
        {
            var ops = expr.Operands;
            if (ops.Length < 2)
                throw new InvalidOperationException("\"%\" requires at least two operands.");

            // Start with first operand
            var first = ExpressionEvaluator.Evaluate(expr, 0, ctx);
            if (!ExpressionEvaluator.IsNumber(first))
                throw new InvalidOperationException(
                    $"\"%\" requires numeric operands, got {first?.GetType().Name}");
            double result = Convert.ToDouble(first, CultureInfo.InvariantCulture);

            // Fold modulo
            for (int i = 1; i < ops.Length; i++)
            {
                var o = ExpressionEvaluator.Evaluate(expr, i, ctx);
                if (!ExpressionEvaluator.IsNumber(o))
                    throw new InvalidOperationException(
                        $"\"%\" requires numeric operands, got {o?.GetType().Name}");
                result %= Convert.ToDouble(o, CultureInfo.InvariantCulture);
            }

            return result;
        }
    }
}