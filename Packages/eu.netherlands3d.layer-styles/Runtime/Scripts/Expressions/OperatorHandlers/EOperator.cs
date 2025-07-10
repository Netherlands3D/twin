using System;
using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.Expressions.OperatorHandlers
{
    public static class EOperator
    {
        public const string Code = "e";
        public static double Evaluate(Expression expr, ExpressionContext ctx) => Math.E;
    }
}