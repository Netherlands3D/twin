using System;
using Netherlands3D.LayerStyles.Expressions;
using UnityEngine;

namespace Netherlands3D.LayerStyles.ExpressionEngine.OperatorHandlers
{
    /// <summary>
    /// ["rgb", number(r), number(g), number(b)]: color
    /// </summary>
    public static class RgbOperator
    {
        public const string Code = "rgb";

        public static Color Evaluate(Expression expression, ExpressionContext context)
        {
            if (expression.Operands.Length != 3)
                throw new InvalidOperationException("\"rgb\" requires exactly 3 operands.");

            float r = (float)ExpressionEvaluator.ToNumber(ExpressionEvaluator.Evaluate(expression, 0, context));
            float g = (float)ExpressionEvaluator.ToNumber(ExpressionEvaluator.Evaluate(expression, 1, context));
            float b = (float)ExpressionEvaluator.ToNumber(ExpressionEvaluator.Evaluate(expression, 2, context));

            if (r < 0 || r > 255) throw new InvalidOperationException($"\"rgb\": red {r} out of range 0–255.");
            if (g < 0 || g > 255) throw new InvalidOperationException($"\"rgb\": green {g} out of range 0–255.");
            if (b < 0 || b > 255) throw new InvalidOperationException($"\"rgb\": blue {b} out of range 0–255.");

            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }
    }
}