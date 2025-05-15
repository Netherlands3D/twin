using System;
using UnityEngine;

namespace Netherlands3D.LayerStyles.Expressions
{
    public abstract class Expr : IExpression
    {
        public string Operator { get; protected set; }
        public IExpression[] Arguments { get; protected set; }
        
        public IConvertible Value { get; protected set; }

        public bool IsLiteral => Operator == Operators.Literal;
        public bool IsExpression => Operator != Operators.Literal;

        public static Expr<bool> EqualsTo<TLeft, TRight>(Expr<TLeft> lhs, Expr<TRight> rhs) where TLeft : IConvertible where TRight : IConvertible
            => new(Operators.EqualTo, new IExpression[] { lhs, rhs });

        public static Expr<bool> GreaterThan<TLeft, TRight>(Expr<TLeft> lhs, Expr<TRight> rhs) where TLeft : IConvertible where TRight : IConvertible
            => new(Operators.GreaterThan, new IExpression[] { lhs, rhs });

        public static Expr<bool> GreaterThanOrEqual<TLeft, TRight>(Expr<TLeft> lhs, Expr<TRight> rhs) where TLeft : IConvertible where TRight : IConvertible
            => new(Operators.GreaterThanOrEqual, new IExpression[] { lhs, rhs });
        
        public static Expr<bool> LessThan<TLeft, TRight>(Expr<TLeft> lhs, Expr<TRight> rhs) where TLeft : IConvertible where TRight : IConvertible
            => new(Operators.LessThan, new IExpression[] { lhs, rhs });
        
        public static Expr<bool> LessThanOrEqual<TLeft, TRight>(Expr<TLeft> lhs, Expr<TRight> rhs) where TLeft : IConvertible where TRight : IConvertible
            => new(Operators.LessThanOrEqual, new IExpression[] { lhs, rhs });
        
        public static Expr<string> Rgb(Expr<int> r, Expr<int> g, Expr<int> b) 
            => new(Operators.Rgb, new IExpression[] { r, g, b });
        
        public static Expr<IConvertible> GetVariable(Expr<string> variableName) 
            => new(Operators.GetVariable, new IExpression[] { variableName });
        
        public static Expr<IConvertible> Min<TLeft, TRight>(Expr<TLeft> lhs, Expr<TRight> rhs) where TLeft : IConvertible where TRight : IConvertible 
            => new(Operators.Min, new IExpression[] { lhs, rhs });
    }

    /// <summary>
    /// Expression definition that follows the expression operators described in https://docs.mapbox.com/style-spec/reference/expressions/,
    /// because why reinvent the wheel?
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Expr<T> : Expr
    {
        // Disallow direct instantiation: Always use the factory methods in the Expr class
        internal Expr(string op, IExpression[] arguments)
        {
            Operator = op;
            Arguments = arguments;
            Value = null;
        }

        // Disallow direct instantiation: Always use the factory methods in the Expr class
        internal Expr(IConvertible value)
        {
            Operator = Operators.Literal;
            Arguments = null;
            Value = value;
        }
        
        public static implicit operator Expr<T>(Expr<IConvertible> v)
        {
            return v.IsLiteral ? new Expr<T>(v.Value) : new Expr<T>(v.Operator, v.Arguments);
        }
        
        public static implicit operator Expr<T>(double v) => new(v);
        public static implicit operator Expr<T>(int v) => new(v);
        public static implicit operator Expr<T>(float v) => new(v);
        public static implicit operator Expr<T>(string v) => new(v);
        public static implicit operator Expr<T>(bool v) => new(v);
        public static implicit operator Expr<T>(Array v) => new(v);
        
        public static implicit operator double(Expr<T> v) => v.Value.ToDouble(null);
        public static implicit operator int(Expr<T> v) => v.Value.ToInt32(null);
        public static implicit operator float(Expr<T> v) => v.Value.ToSingle(null);
        public static implicit operator string(Expr<T> v) => v.Value.ToString(null);
        public static implicit operator bool(Expr<T> v) => v.Value.ToBoolean(null);
        public static implicit operator Array(Expr<T> v) => v.Value as Array;
    }
}

