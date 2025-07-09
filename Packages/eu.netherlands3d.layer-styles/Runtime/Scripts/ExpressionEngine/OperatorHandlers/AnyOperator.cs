using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.ExpressionEngine.OperatorHandlers
{
    public static class AnyOperator
    {
        public const string Code = "any";

        public static bool Evaluate(Expression expression, ExpressionContext context)
        {
            for (int i = 0; i < expression.Operands.Length; i++)
            {
                if (ExpressionEvaluator.AsBool(ExpressionEvaluator.Evaluate(expression, i, context))) return true;
            }

            return false;
        }
    }
}