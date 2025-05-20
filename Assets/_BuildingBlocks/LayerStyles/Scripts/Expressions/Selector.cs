using System;

namespace Netherlands3D.LayerStyles.Expressions
{
    public class Selector : Expr<bool>
    {
        internal Selector(Operators op, IExpression[] arguments) : base(op, arguments)
        {
        }

        internal Selector(ExpressionValue value) : base(value)
        {
        }
    }
}