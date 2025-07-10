using System;
using System.Globalization;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    public static class SubtractOperation
    {
        public const string Code = "-";

        public static double Evaluate(Expression expr, ExpressionContext ctx)
        {
            var ops = expr.Operands;
            if (ops.Length < 1)
                throw new InvalidOperationException("\"-\" requires at least one operand.");

            // Evaluate the first operand
            var first = ExpressionEvaluator.Evaluate(expr, 0, ctx);
            if (!ExpressionEvaluator.IsNumber(first))
                throw new InvalidOperationException(
                    $"\"-\" requires numeric operands, got {first?.GetType().Name}");

            double result = Convert.ToDouble(first, CultureInfo.InvariantCulture);

            // Unary negate
            if (ops.Length == 1)
                return -result;

            // Fold subtraction
            for (int i = 1; i < ops.Length; i++)
            {
                var o = ExpressionEvaluator.Evaluate(expr, i, ctx);
                if (!ExpressionEvaluator.IsNumber(o))
                    throw new InvalidOperationException(
                        $"\"-\" requires numeric operands, got {o?.GetType().Name}");
                result -= Convert.ToDouble(o, CultureInfo.InvariantCulture);
            }

            return result;
        }
    }
}