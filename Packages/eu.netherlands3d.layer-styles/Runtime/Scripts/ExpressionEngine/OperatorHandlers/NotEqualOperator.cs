using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.ExpressionEngine.OperatorHandlers
{
    public static class NotEqualOperator
    {
        public const string Code = "!=";

        public static bool Evaluate(Expression expression, ExpressionContext context)
        {
            return !EqualOperator.Evaluate(expression, context);
        }
    }
}