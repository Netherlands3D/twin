using System;
using UnityEngine;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
/// <summary>
    /// Implements the Mapbox <c>to-hsla</c> expression operator, which converts
    /// a <see cref="Color"/> (or parseable string) into an HSLA component array
    /// [h°, s%, l%, a].
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#to-hsla">
    ///   Mapbox “to-hsla” expression reference
    /// </seealso>
    public static class ToHslaOperation
    {
        /// <summary>The Mapbox operator string for “to-hsla”.</summary>
        public const string Code = "to-hsla";

        /// <summary>
        /// Evaluates the <c>to-hsla</c> expression by parsing its single operand
        /// as a <see cref="Color"/> (or CSS string) and converting to HSLA array.
        /// </summary>
        /// <param name="expression">
        ///   The <see cref="Expression"/> whose sole operand is the color input.
        /// </param>
        /// <param name="context">
        ///   The <see cref="ExpressionContext"/> providing any runtime data.
        /// </param>
        /// <returns>
        ///   An <see cref="object"/>[] of four <see cref="double"/>s: 
        ///   hue (0–360), saturation (0–100), lightness (0–100), alpha (0–1).
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if operand count ≠ 1 or input cannot be interpreted as a color.
        /// </exception>
        public static object[] Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardNumberOfOperands(Code, expression, expected: 1);

            // Parse the single operand into a UnityEngine.Color
            Color color = Operations.GetColorOperand(Code, expression, index: 0, context);

            // Convert RGB → HSLA (h:0–360, s/l:0–100, a:0–1)
            (double hue, double saturation, double lightness) = Operations.ConvertRgbToHsla(color);

            return new object[]
            {
                hue,
                saturation * 100.0,
                Math.Round(lightness * 100.0, 0),
                (double)color.a
            };
        }
    }
}