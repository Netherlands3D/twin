using System;
using UnityEngine;

namespace Netherlands3D.SerializableGisExpressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>rgb</c> expression operator, which builds an
    /// opaque <see cref="Color"/> from red (0–255), green (0–255), and blue (0–255).
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#rgb">
    ///   Mapbox “rgb” expression reference
    /// </seealso>
    public static class RgbOperation
    {
        /// <summary>The Mapbox operator string for “rgb”.</summary>
        public const string Code = "rgb";

        /// <summary>
        /// Evaluates the <c>rgb</c> expression by parsing and validating three
        /// numeric operands (red, green, blue) then converting to a normalized Color.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operands are [red, green, blue].</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing any feature or runtime data.</param>
        /// <returns>A <see cref="Color"/> with each channel in [0,1] and alpha = 1.</returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if operand count ≠ 3 or any component is out of its allowed range.
        /// </exception>
        public static Color Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardNumberOfOperands(Code, expression, expected: 3);

            double red = Operations.GetNumericOperand(Code, "red", expression, 0, context);
            double green = Operations.GetNumericOperand(Code, "green", expression, 1, context);
            double blue = Operations.GetNumericOperand(Code, "blue", expression, 2, context);

            Operations.GuardInRange(Code, "red", red, min: 0, max: 255);
            Operations.GuardInRange(Code, "green", green, min: 0, max: 255);
            Operations.GuardInRange(Code, "blue", blue, min: 0, max: 255);

            return new Color((float)red / 255f, (float)green / 255f, (float)blue / 255f, 1f);
        }
    }
}