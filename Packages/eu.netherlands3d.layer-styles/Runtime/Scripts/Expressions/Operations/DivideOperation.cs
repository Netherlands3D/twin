using System;
using System.Globalization;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>/</c> expression operator, which divides its first numeric
    /// operand by each subsequent numeric operand in turn.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#%2F">
    ///   Mapbox “/” expression reference
    /// </seealso>
    public static class DivideOperation
    {
        /// <summary>The Mapbox operator string for “/”.</summary>
        public const string Code = "/";

        /// <summary>
        /// Evaluates the division expression by folding each operand into the result.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operands are expected to be numeric.</param>
        /// <param name="context">
        ///   The <see cref="ExpressionContext"/> providing any feature or runtime data needed.
        /// </param>
        /// <returns>
        ///   The result of dividing the first operand by each subsequent operand, as a <see cref="double"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if fewer than two operands are provided or any operand is not numeric.
        /// </exception>
        public static double Evaluate(Expression expression, ExpressionContext context)
        {
            var operands = expression.Operands;
            var operandCount = operands.Length;

            if (operandCount < 2)
            {
                throw new InvalidOperationException(
                    $"\"{Code}\" requires at least two operands, got {operandCount}."
                );
            }

            // Evaluate first operand
            var firstValue = ExpressionEvaluator.Evaluate(expression, 0, context);
            if (!ExpressionEvaluator.IsNumber(firstValue))
            {
                throw new InvalidOperationException(
                    $"\"{Code}\" requires numeric operands, got {firstValue?.GetType().Name}"
                );
            }

            double result = Convert.ToDouble(firstValue, CultureInfo.InvariantCulture);

            // Sequentially divide by each subsequent operand
            for (int i = 1; i < operandCount; i++)
            {
                var operandValue = ExpressionEvaluator.Evaluate(expression, i, context);
                if (!ExpressionEvaluator.IsNumber(operandValue))
                {
                    throw new InvalidOperationException(
                        $"\"{Code}\" requires numeric operands, got {operandValue?.GetType().Name}"
                    );
                }

                result /= Convert.ToDouble(operandValue, CultureInfo.InvariantCulture);
            }

            return result;
        }
    }
}


