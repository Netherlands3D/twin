using System;
using System.Globalization;
using Netherlands3D.LayerStyles.Expressions;
using UnityEngine;

namespace Netherlands3D.LayerStyles.Expressions.OperatorHandlers
{
    /// <summary>
    /// ["hsl", number(h°), number(s%), number(l%)]: color
    /// </summary>
    public static class HslOperator
    {
        public const string Code = "hsl";

        public static Color Evaluate(Expression expression, ExpressionContext context)
        {
            if (expression.Operands.Length != 3)
                throw new InvalidOperationException("\"hsl\" requires exactly 3 operands.");

            // read and validate
            double h = Convert.ToDouble(ExpressionEvaluator.Evaluate(expression, 0, context),
                CultureInfo.InvariantCulture);
            double s = Convert.ToDouble(ExpressionEvaluator.Evaluate(expression, 1, context),
                CultureInfo.InvariantCulture);
            double l = Convert.ToDouble(ExpressionEvaluator.Evaluate(expression, 2, context),
                CultureInfo.InvariantCulture);

            if (h < 0 || h > 360) throw new InvalidOperationException($"\"hsl\": hue {h} out of range 0–360.");
            if (s < 0 || s > 100) throw new InvalidOperationException($"\"hsl\": saturation {s} out of range 0–100.");
            if (l < 0 || l > 100) throw new InvalidOperationException($"\"hsl\": lightness {l} out of range 0–100.");

            // normalize
            double hn = h / 360.0;
            double sn = s / 100.0;
            double ln = l / 100.0;

            // HSL → RGB conversion
            double c = (1 - Math.Abs(2 * ln - 1)) * sn;
            double x = c * (1 - Math.Abs((hn * 6 % 2) - 1));
            double m = ln - c / 2;
            double rp = 0, gp = 0, bp = 0;

            if (hn < 1.0 / 6)
            {
                rp = c;
                gp = x;
                bp = 0;
            }
            else if (hn < 2.0 / 6)
            {
                rp = x;
                gp = c;
                bp = 0;
            }
            else if (hn < 3.0 / 6)
            {
                rp = 0;
                gp = c;
                bp = x;
            }
            else if (hn < 4.0 / 6)
            {
                rp = 0;
                gp = x;
                bp = c;
            }
            else if (hn < 5.0 / 6)
            {
                rp = x;
                gp = 0;
                bp = c;
            }
            else
            {
                rp = c;
                gp = 0;
                bp = x;
            }

            return new Color(
                (float)(rp + m),
                (float)(gp + m),
                (float)(bp + m),
                1f
            );
        }
    }
}