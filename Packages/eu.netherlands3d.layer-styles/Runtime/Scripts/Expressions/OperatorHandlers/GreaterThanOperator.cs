using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.Expressions.OperatorHandlers
{
    public static class GreaterThanOperator
    {
        public const string Code = ">";

        public static bool Evaluate(Expression expression, ExpressionContext context)
        {
            return ExpressionEvaluator.Compare(
                ExpressionEvaluator.Evaluate(expression, 0, context),
                ExpressionEvaluator.Evaluate(expression, 1, context)
            ) > 0;
        }
    }
}