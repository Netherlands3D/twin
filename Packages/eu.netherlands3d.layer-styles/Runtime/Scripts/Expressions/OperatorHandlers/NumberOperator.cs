using System;
using System.Globalization;
using System.Linq;
using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.Expressions.OperatorHandlers
{
    /// <summary>
    /// ["number", v₀, fallback₁, …] → first operand that is already a number (error otherwise). 
    /// </summary>
    internal static class NumberOperator
    {
        public const string Code = "number";

        public static double Evaluate(Expression expression, ExpressionContext context)
        {
            foreach (var (operand, idx) in expression.Operands.Select((o, i) => (o, i)))
            {
                var val = ExpressionEvaluator.Evaluate(expression, idx, context);
                if (ExpressionEvaluator.IsNumber(val))
                    return Convert.ToDouble(val, CultureInfo.InvariantCulture);
            }

            throw new InvalidOperationException(
                $"\"number\" assertion failed: no operand evaluated to a number."
            );
        }
    }
}