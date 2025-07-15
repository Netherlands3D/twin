using System;
using UnityEngine;

namespace Netherlands3D.SerializableGisExpressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>to-rgba</c> expression operator, which converts a
    /// <c>color</c> input into an array of four numeric components:
    /// [red (0–255), green (0–255), blue (0–255), alpha (0–1)].
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#to-rgba">
    ///   Mapbox “to-rgba” expression reference
    /// </seealso>
    public static class ToRgbaOperation
    {
        /// <summary>The Mapbox operator string for “to-rgba”.</summary>
        public const string Code = "to-rgba";

        /// <summary>
        /// Evaluates the <c>to-rgba</c> expression by parsing its single operand
        /// as a <see cref="Color"/> (or CSS color string) and returning an array
        /// of four numbers: red, green, blue (0–255), and alpha (0–1).
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose sole operand is the input color.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> for nested evaluation.</param>
        /// <returns>
        ///   An <see cref="object[]"/> of length 4: [red, green, blue, alpha].
        /// </returns>
        public static double[] Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardNumberOfOperands(Code, expression, 1);

            var value = Operations.GetOperandAsColor(Code, expression, 0, context);

            return new[]
            {
                value.r * 255.0, 
                value.g * 255.0,
                value.b * 255.0,
                value.a
            };
        }
    }
}