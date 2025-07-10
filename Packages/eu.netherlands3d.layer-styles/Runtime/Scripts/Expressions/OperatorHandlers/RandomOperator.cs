using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.Expressions.OperatorHandlers
{
    public static class RandomOperator
    {
        public const string Code = "random";
        public static double Evaluate(Expression expr, ExpressionContext ctx) => UnityEngine.Random.value;
    }
}