using System;
using System.Collections;
using Newtonsoft.Json.Linq;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>object</c> expression operator, which returns
    /// the first operand that evaluates to a JSON object (<see cref="JObject"/>)
    /// or a CLR dictionary (<see cref="IDictionary"/>).
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#types-object">
    ///   Mapbox “object” expression reference
    /// </seealso>
    internal static class ObjectOperation
    {
        /// <summary>The Mapbox operator code for “object”.</summary>
        public const string Code = "object";

        /// <summary>
        /// Evaluates the <c>object</c> expression by returning the first operand that evaluates to an object type.
        /// Throws if none is an object.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operands will be tested in order.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing runtime feature data.</param>
        /// <returns>
        ///   The first operand as <see cref="object"/> (either <see cref="JObject"/> or <see cref="IDictionary"/>).
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown if no operand evaluates to an object.</exception>
        public static object Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardAtLeastNumberOfOperands(Code, expression, atLeast: 1);

            int count = expression.Operands.Length;
            for (int i = 0; i < count; i++)
            {
                object raw = ExpressionEvaluator.Evaluate(expression, i, context);
                if (raw is not (JObject or IDictionary)) continue;

                return raw;
            }

            throw new InvalidOperationException($"\"{Code}\" assertion failed: no operand evaluated to an object.");
        }
    }
}
