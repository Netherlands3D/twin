using System;
using System.Globalization;
using System.Linq;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    public static class ToNumberOperation
    {
        public const string Code = "to-number";

        public static double Evaluate(Expression expression, ExpressionContext context)
        {
            foreach (var (_, idx) in expression.Operands.Select((o, i) => (o, i)))
            {
                var o = ExpressionEvaluator.Evaluate(expression, idx, context);
                if (o == null) return 0;
                if (o is bool b) return b ? 1 : 0;
                if (ExpressionEvaluator.IsNumber(o)) return Convert.ToDouble(o, CultureInfo.InvariantCulture);
                if (o is string s &&
                    double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                    return d;
            }

            throw new InvalidOperationException("\"to-number\" failed: no operand could be converted to a number.");
        }
    }
}