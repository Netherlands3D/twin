using System;
using System.Globalization;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>to-number</c> expression operator, which attempts
    /// to convert its operands to a numeric value, returning the first successful
    /// conversion or throwing if none can be converted.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#to-number">
    ///   Mapbox “to-number” expression reference
    /// </seealso>
    public static class ToNumberOperation
    {
        /// <summary>The Mapbox operator string for “to-number”.</summary>
        public const string Code = "to-number";

        /// <summary>
        /// Evaluates the <c>to-number</c> expression by converting each operand
        /// in turn to a number: null ⇒ 0, bool ⇒ 1/0, numeric ⇒ itself, string ⇒ parsed.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operands should be converted.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> used for nested expression evaluation.</param>
        /// <returns>The first successfully converted <see cref="double"/> value.</returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if no operand can be converted to a number.
        /// </exception>
        public static double Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardAtLeastNumberOfOperands(Code, expression, atLeast: 1);

            for (int i = 0; i < expression.Operands.Length; i++)
            {
                object raw = ExpressionEvaluator.Evaluate(expression, i, context);

                if (ExpressionEvaluator.IsNumber(raw)) return Operations.ToDouble(raw);

                switch (raw)
                {
                    case null: return 0;
                    case bool booleanValue: return booleanValue ? 1 : 0;
                    case string stringValue 
                        when double.TryParse(
                            stringValue, 
                            NumberStyles.Any, 
                            CultureInfo.InvariantCulture, 
                            out double parsed
                        ):
                        return parsed;
                }
            }

            throw new InvalidOperationException($"\"{Code}\" failed: no operand could be converted to a number.");
        }
    }
}
