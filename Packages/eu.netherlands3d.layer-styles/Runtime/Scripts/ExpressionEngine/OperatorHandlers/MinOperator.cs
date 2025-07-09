using System.Linq;
using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.ExpressionEngine.OperatorHandlers
{
    public static class MinOperator
    {
        public const string Code = "min";

        public static double Evaluate(Expression expr, ExpressionContext ctx)
        {
            return expr.Operands
                .Select((o, i) => ExpressionEvaluator.ToNumber(ExpressionEvaluator.Evaluate(expr, i, ctx)))
                .Min();
        }
    }
}