using System;
using UnityEngine;

namespace Netherlands3D.LayerStyles.Expressions
{
    public class ExpressionEvaluator
    {
        /// <summary>
        /// When changing this, do not forget to change Expr and ExprJsonConverter 
        /// </summary>
        public IConvertible Evaluate(IExpression expr, ExpressionContext context)
        {
            return expr.Operator switch
            {
                Operators.Literal => expr.Value,
                Operators.EqualTo => EqualTo(context, expr.Arguments[0], expr.Arguments[1]),
                Operators.GreaterThan => GreaterThan(context, expr.Arguments[0], expr.Arguments[1]),
                Operators.GreaterThanOrEqual => GreaterThanOrEqual(context, expr.Arguments[0], expr.Arguments[1]),
                Operators.LessThan => LessThan(context, expr.Arguments[0], expr.Arguments[1]),
                Operators.LessThanOrEqual => LessThanOrEqual(context, expr.Arguments[0], expr.Arguments[1]),
                Operators.Min => Min(context, expr.Arguments[0], expr.Arguments[1]),
                Operators.GetVariable => GetVariable(context, (Expr<string>)expr.Arguments[0]),
                Operators.Rgb => Rgb(
                    context, 
                    (Expr<int>)expr.Arguments[0], 
                    (Expr<int>)expr.Arguments[1], 
                    (Expr<int>)expr.Arguments[2]
                ),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        
        private IConvertible EqualTo(ExpressionContext context, IExpression lhs, IExpression rhs)
        {
            return Equals(Evaluate(rhs, context), Evaluate(lhs, context));
        }

        private IConvertible LessThan(ExpressionContext context, IExpression lhs, IExpression rhs)
        {
            IComparable rhsValue = Evaluate(rhs, context) as IComparable;
            IComparable lhsValue = Evaluate(lhs, context) as IComparable;
            
            return rhsValue?.CompareTo(lhsValue) is 1;
        }

        private IConvertible LessThanOrEqual(ExpressionContext context, IExpression lhs, IExpression rhs)
        {
            IComparable rhsValue = Evaluate(rhs, context) as IComparable;
            IComparable lhsValue = Evaluate(lhs, context) as IComparable;
            
            return rhsValue?.CompareTo(lhsValue) is 0 or 1;
        }

        private IConvertible GreaterThan(ExpressionContext context, IExpression lhs, IExpression rhs)
        {
            IComparable rhsValue = Evaluate(rhs, context) as IComparable;
            IComparable lhsValue = Evaluate(lhs, context) as IComparable;
            
            return rhsValue?.CompareTo(lhsValue) is -1;
        }

        private IConvertible GreaterThanOrEqual(ExpressionContext context, IExpression lhs, IExpression rhs)
        {
            IComparable rhsValue = Evaluate(rhs, context) as IComparable;
            IComparable lhsValue = Evaluate(lhs, context) as IComparable;
            
            return rhsValue?.CompareTo(lhsValue) is 0 or -1;
        }
        
        private IConvertible Min(ExpressionContext context, IExpression lhs, IExpression rhs)
        {
            IComparable rhsValue = Evaluate(rhs, context) as IComparable;
            IComparable lhsValue = Evaluate(lhs, context) as IComparable;
            
            return rhsValue?.CompareTo(lhsValue) is 0 or -1 ? rhs.Value : lhs.Value;
        }
        
        private IConvertible Rgb(
            ExpressionContext context, 
            Expr<int> r, 
            Expr<int> g, 
            Expr<int> b
        ) {
            IConvertible redValue = Evaluate(r, context);
            IConvertible greenValue = Evaluate(g, context);
            IConvertible blueValue = Evaluate(b, context);
            
            return ColorUtility.ToHtmlStringRGB(
                new Color(
                    redValue.ToSingle(null) / 255f, 
                    greenValue.ToSingle(null) / 255f, 
                    blueValue.ToSingle(null) / 255f
                )
            );
        }
        
        private IConvertible GetVariable(ExpressionContext context, Expr<string> id)
        {
            return context.Feature.GetAttribute(Evaluate(id, context).ToString(null));
        }
    }
}