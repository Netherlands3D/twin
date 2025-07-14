using System;
using System.Globalization;
using UnityEngine;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>hsl</c> expression operator, which builds an
    /// opaque <see cref="Color"/> from hue (0–360°), saturation (0–100%),
    /// and lightness (0–100%).
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#hsl">
    ///   Mapbox “hsl” expression reference
    /// </seealso>
    public static class HslOperation
    {
        /// <summary>The Mapbox operator string for “hsl”.</summary>
        public const string Code = "hsl";

        /// <summary>
        /// Evaluates the <c>hsl</c> expression by parsing and validating its
        /// three numeric operands, then converting to an RGB <see cref="Color"/>.
        /// </summary>
        /// <param name="expression">
        ///   The <see cref="Expression"/> whose operands are [hue, saturation, lightness].
        /// </param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing any runtime data.</param>
        /// <returns>The resulting opaque <see cref="Color"/> (alpha = 1).</returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if operand count is not 3 or any component is out of range.
        /// </exception>
        public static Color Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardNumberOfOperands(Code, expression, expected: 3);

            double hue = Operations.GetNumericOperand(Code,"hue", expression, 0, context);
            double saturation = Operations.GetNumericOperand(Code, "saturation", expression, 1, context);
            double lightness = Operations.GetNumericOperand(Code, "lightness", expression, 2, context);

            Operations.GuardInRange(Code, "hue", hue, 0, 360);
            Operations.GuardInRange(Code, "saturation", saturation, 0, 100);
            Operations.GuardInRange(Code, "lightness", lightness, 0, 100);

            return ConvertHslToRgb(hue, saturation, lightness);
        }

        private static Color ConvertHslToRgb(double hue, double saturation, double lightness) 
        {
            return HslaOperation.ConvertHslaToRgba(hue, saturation, lightness, alpha: 1.0);
        }
    }
}