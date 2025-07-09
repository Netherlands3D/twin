using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.ExpressionEngine.OperatorHandlers
{
    public static class EqualOperator
    {
        public const string Code = "==";

        public static bool Evaluate(Expression expression, ExpressionContext context)
        {
            return ExpressionEvaluator.IsEqual(
                ExpressionEvaluator.Evaluate(expression, 0, context),
                ExpressionEvaluator.Evaluate(expression, 1, context)
            );
        }
    }
}