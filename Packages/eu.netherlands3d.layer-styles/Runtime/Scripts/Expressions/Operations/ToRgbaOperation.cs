using System;
using UnityEngine;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// ["to-rgba", color]: array<number,4>
    /// </summary>
    public static class ToRgbaOperation
    {
        public const string Code = "to-rgba";

        public static object[] Evaluate(Expression expression, ExpressionContext context)
        {
            if (expression.Operands.Length != 1)
                throw new InvalidOperationException("\"to-rgba\" requires exactly 1 operand.");

            // get color or parse string
            var val = ExpressionEvaluator.Evaluate(expression, 0, context);
            Color c;
            if (val is Color col) c = col;
            else if (val is string s && ColorUtility.TryParseHtmlString(s, out col)) c = col;
            else throw new InvalidOperationException("\"to-rgba\" requires a color input.");

            // output R,G,B (0–255), A (0–1)
            return new object[]
            {
                (double)(c.r * 255.0),
                (double)(c.g * 255.0),
                (double)(c.b * 255.0),
                (double)c.a
            };
        }
    }
}