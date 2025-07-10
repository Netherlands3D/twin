using System;
using Netherlands3D.LayerStyles.Expressions;
using UnityEngine;

namespace Netherlands3D.LayerStyles.Expressions.OperatorHandlers
{
    /// <summary>
    /// ["to-hsla", color]: array<number,4>
    /// </summary>
    public static class ToHslaOperator
    {
        public const string Code = "to-hsla";

        public static object[] Evaluate(Expression expression, ExpressionContext context)
        {
            if (expression.Operands.Length != 1)
                throw new InvalidOperationException("\"to-hsla\" requires exactly 1 operand.");

            // get color or parse string
            var val = ExpressionEvaluator.Evaluate(expression, 0, context);
            Color c;
            if (val is Color col) c = col;
            else if (val is string str && ColorUtility.TryParseHtmlString(str, out col)) c = col;
            else throw new InvalidOperationException("\"to-hsla\" requires a color input.");

            // RGB → HSL
            float r = c.r, g = c.g, b = c.b;
            float max = Mathf.Max(r, Mathf.Max(g, b));
            float min = Mathf.Min(r, Mathf.Min(g, b));
            float d = max - min;
            float l = (max + min) / 2f;

            float h, s;
            if (d == 0f)
            {
                h = 0f;
                s = 0f;
            }
            else
            {
                s = d / (1f - Mathf.Abs(2f * l - 1f));
                if (max == r)
                    h = 60f * (((g - b) / d) % 6f);
                else if (max == g)
                    h = 60f * (((b - r) / d) + 2f);
                else
                    h = 60f * (((r - g) / d) + 4f);
                if (h < 0) h += 360f;
            }

            // map back to spec units: H° 0–360, S%/L% 0–100, A 0–1
            return new object[]
            {
                (double)h,
                (double)(s * 100.0),
                (double)(l * 100.0),
                (double)c.a
            };
        }
    }
}