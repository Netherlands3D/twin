using System;
using UnityEngine;

namespace Netherlands3D.LayerStyles.Expressions.Operations
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

            object rawValue = ExpressionEvaluator.Evaluate(expression, 0, context);

            var value = ConvertToColor(rawValue);
            if (value == null)
            {
                throw new InvalidOperationException(
                    $"\"{Code}\" requires a color input, got {rawValue?.GetType().Name}."
                );
            }

            Color colorValue = value.Value;
            return new[]
            {
                colorValue.r * 255.0, 
                colorValue.g * 255.0,
                colorValue.b * 255.0,
                colorValue.a
            };
        }

        private static Color? ConvertToColor(object rawValue)
        {
            // performant check
            if (rawValue is Color c) return c;

            // guards
            if (rawValue is not string s) return null;
            if (!ColorUtility.TryParseHtmlString(s, out c)) return null;
            
            // it is a converted color
            return c;
        }
    }
}