using System;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>get</c> expression operator, which retrieves
    /// the value of a named property from the current <c>LayerFeature</c>.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#get">
    ///   Mapbox “get” expression reference
    /// </seealso>
    public static class GetOperation
    {
        /// <summary>The Mapbox operator string for “get”.</summary>
        public const string Code = "get";

        /// <summary>
        /// Evaluates the <c>get</c> expression by fetching the feature attribute.
        /// </summary>
        /// <param name="expression">
        ///   The <see cref="Expression"/> whose first operand should evaluate
        ///   to a string key naming the attribute to retrieve.
        /// </param>
        /// <param name="context">
        ///   The <see cref="ExpressionContext"/> containing the <c>LayerFeature</c>
        ///   whose attributes are accessed.
        /// </param>
        /// <returns>
        ///   The attribute value as a <see cref="string"/>, or <c>null</c> if not present.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if the <paramref name="context"/> or its <c>Feature</c> is null,
        ///   or if the attribute key does not evaluate to a string.
        /// </exception>
        public static string Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardNumberOfOperands(Code, expression, 1);

            if (context?.Feature == null)
            {
                throw new InvalidOperationException($"{Code} requires a non-null ExpressionContext with a Feature.");
            }

            var rawKey = ExpressionEvaluator.Evaluate(expression, 0, context);
            var attributeKey = rawKey?.ToString();

            if (attributeKey == null) throw new InvalidOperationException($"{Code}: attribute key must be a string.");

            return context.Feature.GetAttribute(attributeKey);
        }
    }
}
