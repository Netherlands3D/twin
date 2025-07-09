using System;
using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.ExpressionEngine.OperatorHandlers
{
    public static class PiOperator
    {
        public const string Code = "pi";
        public static double Evaluate(Expression expr, ExpressionContext ctx) => Math.PI;
    }
}