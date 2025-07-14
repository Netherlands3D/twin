using System;
using UnityEngine;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>hsla</c> expression operator, which builds an
    /// RGBA <see cref="Color"/> from hue (0–360°), saturation (0–100%), lightness
    /// (0–100%), and alpha (0–1).
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#hsla">
    ///   Mapbox “hsla” expression reference
    /// </seealso>
    public static class HslaOperation
    {
        /// <summary>Mapbox operator code for “hsla”.</summary>
        public const string Code = "hsla";

        /// <summary>
        /// Evaluates the <c>hsla</c> expression by parsing the four numeric
        /// components, validating them, and converting to an RGBA <see cref="Color"/>.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operands are [h, s, l, a].</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing any runtime data.</param>
        /// <returns>The resulting <see cref="Color"/>.</returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if operand count is not 4 or any component is out of range.
        /// </exception>
        public static Color Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardNumberOfOperands(Code, expression, expected: 4);

            double hue = Operations.GetNumericOperand(Code, "hue", expression, 0, context);
            double saturation = Operations.GetNumericOperand(Code, "saturation", expression, 1, context);
            double lightness = Operations.GetNumericOperand(Code,"lightness", expression, 2, context);
            double alpha = Operations.GetNumericOperand(Code, "alpha", expression, 3, context);

            Operations.GuardInRange(Code, "hue", hue, 0, 360);
            Operations.GuardInRange(Code, "saturation", saturation, 0, 100);
            Operations.GuardInRange(Code, "lightness", lightness, 0, 100);
            Operations.GuardInRange(Code, "alpha", alpha, 0, 1);

            return ConvertHslaToRgba(hue, saturation, lightness, alpha);
        }

        // TODO: Review this in depth - I need to verify whether this algorithm is correct
        internal static Color ConvertHslaToRgba(double hue, double saturation, double lightness, double alpha)
        {
            float hue01 = (float)(hue / 360.0);
            float saturation01 = (float)(saturation / 100.0);
            float lightness01 = (float)(lightness / 100.0);

            float value = lightness01 + saturation01 * Mathf.Min(lightness01, 1f - lightness01);
            saturation01 = (value == 0f) ? 0f : 2f * (1f - lightness01 / value);

            Color rgb = Color.HSVToRGB(hue01, saturation01, value, hdr: false);
            rgb.a = (float)alpha;

            return rgb;
        }
    }
}