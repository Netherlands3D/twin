using System;
using System.Linq;
using Netherlands3D.LayerStyles.Expressions;
using UnityEngine;

namespace Netherlands3D.LayerStyles.ExpressionEngine.OperatorHandlers
{
    public static class ToColorOperator
    {
        public const string Code = "to-color";

        public static Color Evaluate(Expression expression, ExpressionContext context)
        {
            var operands = expression.Operands.Select((o, i) => (o, i));
            foreach (var (_, idx) in operands)
            {
                var operand = ExpressionEvaluator.Evaluate(expression, idx, context);
                if (operand is Color c) return c;

                if (operand is string str && ColorUtility.TryParseHtmlString(str, out c))
                    return c;
            }

            throw new InvalidOperationException($"\"to-color\" failed: no operand could be converted to a color.");
        }
    }
}