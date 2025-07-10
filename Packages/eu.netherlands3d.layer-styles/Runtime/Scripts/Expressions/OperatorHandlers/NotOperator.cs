using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.Expressions.OperatorHandlers
{
    public static class NotOperator
    {
        public const string Code = "!";

        public static bool Evaluate(Expression expression, ExpressionContext context)
        {
            return !ExpressionEvaluator.AsBool(ExpressionEvaluator.Evaluate(expression, 0, context));
        }
    }
}