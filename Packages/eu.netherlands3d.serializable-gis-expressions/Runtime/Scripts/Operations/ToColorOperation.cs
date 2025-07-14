using System;
using UnityEngine;

namespace Netherlands3D.SerializableGisExpressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>to-color</c> expression operator, which
    /// converts its operands to a <see cref="Color"/> by first returning any
    /// <see cref="Color"/> value, or by parsing a CSS string.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#to-color">
    ///   Mapbox “to-color” expression reference
    /// </seealso>
    public static class ToColorOperation
    {
        /// <summary>The Mapbox operator string for “to-color”.</summary>
        public const string Code = "to-color";

        /// <summary>
        /// Evaluates the <c>to-color</c> expression by checking each operand:
        /// returns the first <see cref="Color"/>, or parses the first valid
        /// HTML/CSS color string.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operands are tested in order.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing any runtime data.</param>
        /// <returns>The first convertible <see cref="Color"/>.</returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if no operand can be converted to a color.
        /// </exception>
        public static Color Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardAtLeastNumberOfOperands(Code, expression, 1);

            for (int i = 0; i < expression.Operands.Length; i++)
            {
                object raw = ExpressionEvaluator.Evaluate(expression, i, context);

                switch (raw)
                {
                    case Color directColor: return directColor;
                    case string str when ColorUtility.TryParseHtmlString(str, out Color parsed):
                        return parsed;
                }
            }

            throw new InvalidOperationException($"\"{Code}\" failed: no operand could be converted to a color.");
        }
    }
}
