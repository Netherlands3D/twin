using Newtonsoft.Json;

namespace Netherlands3D.LayerStyles.Expressions
{
    [JsonConverter(typeof(ExprJsonConverter))]
    public abstract class Expr : IExpression
    {
        public Operators Operator { get; protected set; }
        public IExpression[] Arguments { get; protected set; }
        
        public ExpressionValue Value { get; protected set; }

        public bool IsLiteral => Operator == Operators.Literal;

        public bool IsExpression => Operator != Operators.Literal;

        #region Operators - when you change this, change ExpressionEvaluator and ExprJsonConverter too!
        
        /// <summary>
        /// Compares the values from both expressions and returns true if they are equal. Do note that
        /// this is a strict-type comparison, comparing expressions of different types will always result in false.
        /// </summary>
        /// <see href="https://docs.mapbox.com/style-spec/reference/expressions/#%3D%3D"/>
        public static Expr<bool> EqualsTo(Expr<ExpressionValue> lhs, Expr<ExpressionValue> rhs)
            => new(Operators.EqualTo, new IExpression[] { lhs, rhs });

        public static Expr<bool> GreaterThan(Expr<ExpressionValue> lhs, Expr<ExpressionValue> rhs)
            => new(Operators.GreaterThan, new IExpression[] { lhs, rhs });

        public static Expr<bool> GreaterThanOrEqual(Expr<ExpressionValue> lhs, Expr<ExpressionValue> rhs)
            => new(Operators.GreaterThanOrEqual, new IExpression[] { lhs, rhs });
        
        public static Expr<bool> LessThan(Expr<ExpressionValue> lhs, Expr<ExpressionValue> rhs)
            => new(Operators.LessThan, new IExpression[] { lhs, rhs });
        
        public static Expr<bool> LessThanOrEqual(Expr<ExpressionValue> lhs, Expr<ExpressionValue> rhs)
            => new(Operators.LessThanOrEqual, new IExpression[] { lhs, rhs });
        
        public static Expr<string> Rgb(Expr<int> r, Expr<int> g, Expr<int> b) 
            => new(Operators.Rgb, new IExpression[] { r, g, b });
        
        public static Expr<ExpressionValue> GetVariable(Expr<string> variableName) 
            => new(Operators.GetVariable, new IExpression[] { variableName });
        
        public static Expr<ExpressionValue> Min(Expr<ExpressionValue> lhs, Expr<ExpressionValue> rhs) 
            => new(Operators.Min, new IExpression[] { lhs, rhs });

        public static Expr<bool> In(Expr<ExpressionValue> keyword, Expr<ExpressionValue> array)
            => new(Operators.In, new IExpression[] { keyword, array });

        #endregion
    }

    /// <summary>
    /// Expression definition that follows the expression operators described in https://docs.mapbox.com/style-spec/reference/expressions/,
    /// because why reinvent the wheel?
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Expr<T> : Expr
    {
        // Disallow direct instantiation: Always use the factory methods in the Expr class
        internal Expr(Operators op, IExpression[] arguments)
        {
            Operator = op;
            Arguments = arguments;
            Value = null;
        }

        // Disallow direct instantiation: Always use the factory methods in the Expr class
        internal Expr(ExpressionValue value)
        {
            Operator = Operators.Literal;
            Arguments = null;
            Value = value;
        }
        
        public static implicit operator Expr<T>(Expr<ExpressionValue> v)
        {
            return v.IsLiteral ? new Expr<T>(v.Value) : new Expr<T>(v.Operator, v.Arguments);
        }
        
        public static implicit operator Expr<T>(double v) => new(v);
        public static implicit operator Expr<T>(int v) => new(v);
        public static implicit operator Expr<T>(float v) => new(v);
        public static implicit operator Expr<T>(string v) => new(v);
        public static implicit operator Expr<T>(bool v) => new(v);
        public static implicit operator Expr<T>(ExpressionValue[] v) => new(v);
        
        public static implicit operator double(Expr<T> v) => v.Value.ToDouble(null);
        public static implicit operator int(Expr<T> v) => v.Value.ToInt32(null);
        public static implicit operator float(Expr<T> v) => v.Value.ToSingle(null);
        public static implicit operator string(Expr<T> v) => v.Value.ToString(null);
        public static implicit operator bool(Expr<T> v) => v.Value.ToBoolean(null);
        public static implicit operator ExpressionValue[](Expr<T> v) => v.Value.IsCollection ? v.Value : null;
        
        public static implicit operator Expr<ExpressionValue>(Expr<T> v)
            => v.IsLiteral
                ? new Expr<ExpressionValue>(v.Value)
                : new Expr<ExpressionValue>(v.Operator, v.Arguments);
        
        public static Expr<T> TryParse(IExpression v)
            => v.IsLiteral
                ? new Expr<T>(v.Value)
                : new Expr<T>(v.Operator, v.Arguments);
    }
}

