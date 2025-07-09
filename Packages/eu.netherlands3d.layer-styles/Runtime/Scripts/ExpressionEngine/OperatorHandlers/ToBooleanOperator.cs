using System;
using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.ExpressionEngine.OperatorHandlers
{
    public static class ToBooleanOperator
    {
        public const string Code = "to-boolean";

        public static bool Evaluate(Expression expression, ExpressionContext context)
        {
            var operand = ExpressionEvaluator.Evaluate(expression, 0, context);
            if (operand == null) return false;
            if (operand is bool b) return b;
            if (operand is string s) return s.Length > 0;
            if (ExpressionEvaluator.IsNumber(operand))
            {
                var d = Convert.ToDouble(operand);
                return d is not (0 or double.NaN);
            }

            if (operand is object[] arr) return arr.Length > 0;

            return true; // anything else → truthy
        }
    }
}