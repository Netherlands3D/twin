using System;
using UnityEngine;

namespace Netherlands3D.LayerStyles.Expressions
{
    public class ExpressionEvaluator
    {
        /// <summary>
        /// When changing this, do not forget to change Expr 
        /// </summary>
        public ExpressionValue Evaluate(IExpression expr, ExpressionContext context)
        {
            if (expr.IsValue)
            {
                return expr.Value;
            }
            
            return expr.Operator switch
            {
                Operators.EqualTo => EqualTo(context, expr.Arguments[0], expr.Arguments[1]),
                Operators.GreaterThan => GreaterThan(context, expr.Arguments[0], expr.Arguments[1]),
                Operators.GreaterThanOrEqual => GreaterThanOrEqual(context, expr.Arguments[0], expr.Arguments[1]),
                Operators.LessThan => LessThan(context, expr.Arguments[0], expr.Arguments[1]),
                Operators.LessThanOrEqual => LessThanOrEqual(context, expr.Arguments[0], expr.Arguments[1]),
                Operators.Min => Min(context, expr.Arguments[0], expr.Arguments[1]),
                Operators.GetVariable => GetVariable(context, (Expr<string>)expr.Arguments[0]),
                Operators.Rgb => Rgb(
                    context, 
                    expr.Arguments[0] as Expr<int>, 
                    expr.Arguments[1] as Expr<int>, 
                    expr.Arguments[2] as Expr<int>
                )
            };
        }
        
        private ExpressionValue EqualTo(ExpressionContext context, IExpression lhs, IExpression rhs)
        {
            return Equals(Evaluate(rhs, context), Evaluate(lhs, context));
        }

        private ExpressionValue LessThan(ExpressionContext context, IExpression lhs, IExpression rhs)
        {
            var lhsValue = Evaluate(lhs, context);
            var rhsValue = Evaluate(rhs, context);

            return rhsValue.CompareTo(lhsValue) is 1;
        }

        private ExpressionValue LessThanOrEqual(ExpressionContext context, IExpression lhs, IExpression rhs)
        {
            var lhsValue = Evaluate(lhs, context);
            var rhsValue = Evaluate(rhs, context);

            return rhsValue.CompareTo(lhsValue) is 0 or 1;
        }

        private ExpressionValue GreaterThan(ExpressionContext context, IExpression lhs, IExpression rhs)
        {
            var lhsValue = Evaluate(lhs, context);
            var rhsValue = Evaluate(rhs, context);

            return rhsValue.CompareTo(lhsValue) is -1;
        }

        private ExpressionValue GreaterThanOrEqual(ExpressionContext context, IExpression lhs, IExpression rhs)
        {
            var lhsValue = Evaluate(lhs, context);
            var rhsValue = Evaluate(rhs, context);

            return rhsValue.CompareTo(lhsValue) is 0 or -1;
        }
        
        private ExpressionValue Min(ExpressionContext context, IExpression lhs, IExpression rhs)
        {
            var lhsValue = Evaluate(lhs, context);
            var rhsValue = Evaluate(rhs, context);

            return rhsValue.CompareTo(lhsValue) is 0 or -1 ? rhsValue : lhsValue;
        }
        
        private ExpressionValue Rgb(
            ExpressionContext context, 
            Expr<int> r, 
            Expr<int> g, 
            Expr<int> b
        ) {
            ExpressionValue redValue = Evaluate(r, context);
            ExpressionValue greenValue = Evaluate(g, context);
            ExpressionValue blueValue = Evaluate(b, context);
            
            return ColorUtility.ToHtmlStringRGB(
                new Color(
                    (int)redValue / 255f, 
                    (int)greenValue / 255f, 
                    (int)blueValue / 255f
                )
            );
        }
        
        private ExpressionValue GetVariable(ExpressionContext context, Expr<string> id)
        {
            return context.Feature.GetAttribute(Evaluate(id, context));
        }
    }
}