using System;
using System.Globalization;
using Netherlands3D.LayerStyles.Expressions;
using UnityEngine;

namespace Netherlands3D.LayerStyles.ExpressionEngine.OperatorHandlers
{
    /// <summary>
    /// ["rgba", number(r), number(g), number(b), number(a)]: color
    /// </summary>
    public static class RgbaOperator
    {
        public const string Code = "rgba";

        public static Color Evaluate(Expression expression, ExpressionContext context)
        {
            if (expression.Operands.Length != 4)
                throw new InvalidOperationException("\"rgba\" requires exactly 4 operands.");

            float r = (float)Convert.ToDouble(ExpressionEvaluator.Evaluate(expression, 0, context),
                CultureInfo.InvariantCulture);
            float g = (float)Convert.ToDouble(ExpressionEvaluator.Evaluate(expression, 1, context),
                CultureInfo.InvariantCulture);
            float b = (float)Convert.ToDouble(ExpressionEvaluator.Evaluate(expression, 2, context),
                CultureInfo.InvariantCulture);
            float a = (float)Convert.ToDouble(ExpressionEvaluator.Evaluate(expression, 3, context),
                CultureInfo.InvariantCulture);

            if (r < 0 || r > 255) throw new InvalidOperationException($"\"rgba\": red {r} out of range 0–255.");
            if (g < 0 || g > 255) throw new InvalidOperationException($"\"rgba\": green {g} out of range 0–255.");
            if (b < 0 || b > 255) throw new InvalidOperationException($"\"rgba\": blue {b} out of range 0–255.");
            if (a < 0 || a > 1) throw new InvalidOperationException($"\"rgba\": alpha {a} out of range 0–1.");

            return new Color(r / 255f, g / 255f, b / 255f, a);
        }
    }
}