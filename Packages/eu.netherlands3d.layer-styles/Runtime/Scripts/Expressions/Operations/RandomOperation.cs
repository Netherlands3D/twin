using System;
using System.Globalization;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>random</c> expression operator, which returns a pseudo-random
    /// number in the range [min, max) based on a seed value.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#random">
    ///   Mapbox “random” expression reference
    /// </seealso>
    public static class RandomOperation
    {
        /// <summary>The Mapbox operator string for “random”.</summary>
        public const string Code = "random";

        /// <summary>
        /// Evaluates the <c>random</c> expression by generating a pseudo-random double
        /// ≥ <c>min</c> and &lt; <c>max</c>, using the provided <c>seed</c>.
        /// </summary>
        /// <param name="expression">
        ///   The <see cref="Expression"/> whose operands are [min, max, seed].
        /// </param>
        /// <param name="context">The <see cref="ExpressionContext"/> for evaluation.</param>
        /// <returns>A pseudo-random double in [min, max).</returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if the operand count isn’t 3, or if min/max aren’t numbers,
        ///   or if seed is neither a number nor a string.
        /// </exception>
        public static double Evaluate(Expression expression, ExpressionContext context)
        {
            // exactly three operands: min, max, seed
            Operations.GuardNumberOfOperands(Code, expression, expected: 3);

            double min = Operations.GetNumericOperand(Code, "min", expression, 0, context);
            double max = Operations.GetNumericOperand(Code, "max", expression, 1, context);
            int seed = ComputeSeed(ExpressionEvaluator.Evaluate(expression, 2, context));

            var rng = new Random(seed);
            double sample = rng.NextDouble();
            return min + sample * (max - min);
        }

        private static int ComputeSeed(object rawSeed)
        {
            if (rawSeed is string s) return s.GetHashCode();

            if (Operations.IsNumber(rawSeed)) return Convert.ToInt32(rawSeed, CultureInfo.InvariantCulture);

            throw new InvalidOperationException(
                $"\"{Code}\" seed must be a number or string, got {rawSeed?.GetType().Name}."
            );
        }
    }
}
