using System;
using UnityEngine;

namespace Netherlands3D.SerializableGisExpressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>rgba</c> expression operator, which builds a
    /// <see cref="Color"/> from red (0–255), green (0–255), blue (0–255), and
    /// alpha (0–1).
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#rgba">
    ///   Mapbox “rgba” expression reference
    /// </seealso>
    public static class RgbaOperation
    {
        /// <summary>The Mapbox operator string for “rgba”.</summary>
        public const string Code = "rgba";

        /// <summary>
        /// Evaluates the <c>rgba</c> expression by parsing and validating four numeric operands (red, green, blue,
        /// alpha) then converting to a normalized Color.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operands are [red, green, blue, alpha].</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing any feature or runtime data.</param>
        /// <returns>A <see cref="Color"/> with each channel in [0,1] and alpha in [0,1].</returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if operand count ≠ 4 or any component is out of its allowed range.
        /// </exception>
        public static Color Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardNumberOfOperands(Code, expression, expected: 4);

            double red = Operations.GetOperandAsNumber(Code, "red", expression, 0, context);
            double green = Operations.GetOperandAsNumber(Code, "green", expression, 1, context);
            double blue = Operations.GetOperandAsNumber(Code, "blue", expression, 2, context);
            double alpha = Operations.GetOperandAsNumber(Code, "alpha", expression, 3, context);

            Operations.GuardInRange(Code, "red", red, min: 0, max: 255);
            Operations.GuardInRange(Code, "green", green, min: 0, max: 255);
            Operations.GuardInRange(Code, "blue", blue, min: 0, max: 255);
            Operations.GuardInRange(Code, "alpha", alpha, min: 0, max: 1);

            return new Color((float)red / 255f, (float)green / 255f, (float)blue / 255f, (float)alpha);
        }
    }
}