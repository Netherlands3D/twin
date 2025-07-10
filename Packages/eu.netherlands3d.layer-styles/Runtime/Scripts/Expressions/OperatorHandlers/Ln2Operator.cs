using System;
using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.Expressions.OperatorHandlers
{
    public static class Ln2Operator
    {
        public const string Code = "ln2";
        public static double Evaluate(Expression expr, ExpressionContext ctx) => Math.Log(2.0);
    }
}