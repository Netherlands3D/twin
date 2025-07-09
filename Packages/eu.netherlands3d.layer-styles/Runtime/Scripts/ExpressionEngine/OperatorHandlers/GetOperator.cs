using System;
using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.ExpressionEngine.OperatorHandlers
{
    public static class GetOperator
    {
        public const string Code = "get";

        public static string Evaluate(Expression expression, ExpressionContext context)
        {
            if (context?.Feature == null)
            {
                throw new InvalidOperationException("Get requires a non-null ExpressionContext with a Feature.");
            }

            var keyObj = ExpressionEvaluator.Evaluate(expression, 0, context);
            var key = keyObj?.ToString();
            if (key == null)
            {
                throw new InvalidOperationException("Get: attribute key must be a string.");
            }

            return context.Feature.GetAttribute(key);
        }
    }
}