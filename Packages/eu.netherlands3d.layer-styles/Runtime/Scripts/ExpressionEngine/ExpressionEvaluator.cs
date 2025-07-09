using System;
using Netherlands3D.LayerStyles.Expressions;
using Op = Netherlands3D.LayerStyles.ExpressionEngine.OperatorHandlers;

namespace Netherlands3D.LayerStyles.ExpressionEngine
{
    public static class ExpressionEvaluator
    {
        public static ExpressionValue Evaluate(string primitive, ExpressionContext context = null)
        {
            return new ExpressionValue(primitive);
        }

        public static ExpressionValue Evaluate(bool primitive, ExpressionContext context = null)
        {
            return new ExpressionValue(primitive);
        }

        public static ExpressionValue Evaluate(int primitive, ExpressionContext context = null)
        {
            return new ExpressionValue(primitive);
        }

        public static ExpressionValue Evaluate(float primitive, ExpressionContext context = null)
        {
            return new ExpressionValue(primitive);
        }

        public static ExpressionValue Evaluate(double primitive, ExpressionContext context = null)
        {
            return new ExpressionValue(primitive);
        }

        public static ExpressionValue Evaluate(object[] primitive, ExpressionContext context = null)
        {
            return new ExpressionValue(primitive);
        }

        public static ExpressionValue Evaluate(Expression expression, ExpressionContext context = null)
        {
            var result = expression.Operator switch
            {
                // https://docs.mapbox.com/style-spec/reference/expressions/#types
                Expression.Operators.Array => Op.ArrayOperator.Evaluate(expression, context),
                Expression.Operators.Boolean => Op.BooleanOperator.Evaluate(expression, context),
                Expression.Operators.Literal => Op.LiteralOperator.Evaluate(expression, context),
                Expression.Operators.Number => Op.NumberOperator.Evaluate(expression, context),
                Expression.Operators.NumberFormat => Op.NumberFormatOperator.Evaluate(expression, context),
                Expression.Operators.Object => Op.ObjectOperator.Evaluate(expression, context),
                Expression.Operators.String => Op.StringOperator.Evaluate(expression, context),
                Expression.Operators.ToBoolean => Op.ToBooleanOperator.Evaluate(expression, context),
                Expression.Operators.ToColor => Op.ToColorOperator.Evaluate(expression, context),
                Expression.Operators.ToNumber => Op.ToNumberOperator.Evaluate(expression, context),
                Expression.Operators.ToString => Op.ToStringOperator.Evaluate(expression, context),
                Expression.Operators.TypeOf => Op.TypeOfOperator.Evaluate(expression, context),
                
                // https://docs.mapbox.com/style-spec/reference/expressions/#lookup
                Expression.Operators.Get => Op.GetOperator.Evaluate(expression, context),

                // https://docs.mapbox.com/style-spec/reference/expressions/#decision
                Expression.Operators.Not => Op.NotOperator.Evaluate(expression, context),
                Expression.Operators.NotEqual => Op.NotEqualOperator.Evaluate(expression, context),
                Expression.Operators.LessThan => Op.LessThanOperator.Evaluate(expression, context),
                Expression.Operators.LessThanOrEqual => Op.LessThanOrEqualOperator.Evaluate(expression, context),
                Expression.Operators.Equal => Op.EqualOperator.Evaluate(expression, context),
                Expression.Operators.GreaterThan => Op.GreaterThanOperator.Evaluate(expression, context),
                Expression.Operators.GreaterThanOrEqual => Op.GreaterThanOrEqualOperator.Evaluate(expression,
                    context),
                Expression.Operators.All => Op.AllOperator.Evaluate(expression, context),
                Expression.Operators.Any => Op.AnyOperator.Evaluate(expression, context),
                
                // https://docs.mapbox.com/style-spec/reference/expressions/#color
                Expression.Operators.Hsl => Op.HslOperator.Evaluate(expression, context),
                Expression.Operators.Hsla => Op.HslaOperator.Evaluate(expression, context),
                Expression.Operators.Rgb => Op.RgbOperator.Evaluate(expression, context),
                Expression.Operators.Rgba => Op.RgbaOperator.Evaluate(expression, context),
                Expression.Operators.ToHsla => Op.ToHslaOperator.Evaluate(expression, context),
                Expression.Operators.ToRgba => Op.ToRgbaOperator.Evaluate(expression, context),
                
                // https://docs.mapbox.com/style-spec/reference/expressions/#math
                // arithmetic
                Expression.Operators.Add => Op.AddOperator.Evaluate(expression, context),
                Expression.Operators.Subtract => Op.SubtractOperator.Evaluate(expression, context),
                Expression.Operators.Multiply => Op.MultiplyOperator.Evaluate(expression, context),
                Expression.Operators.Divide => Op.DivideOperator.Evaluate(expression, context),
                Expression.Operators.Modulo => Op.ModuloOperator.Evaluate(expression, context),
                Expression.Operators.Power => Op.PowerOperator.Evaluate(expression, context),

                // unary math
                Expression.Operators.Abs => Op.AbsOperator.Evaluate(expression, context),
                Expression.Operators.Ceil => Op.CeilOperator.Evaluate(expression, context),
                Expression.Operators.Floor => Op.FloorOperator.Evaluate(expression, context),
                Expression.Operators.Round => Op.RoundOperator.Evaluate(expression, context),
                Expression.Operators.Sqrt => Op.SqrtOperator.Evaluate(expression, context),

                // trigonometry
                Expression.Operators.Acos => Op.AcosOperator.Evaluate(expression, context),
                Expression.Operators.Asin => Op.AsinOperator.Evaluate(expression, context),
                Expression.Operators.Atan => Op.AtanOperator.Evaluate(expression, context),
                Expression.Operators.Cos => Op.CosOperator.Evaluate(expression, context),
                Expression.Operators.Sin => Op.SinOperator.Evaluate(expression, context),
                Expression.Operators.Tan => Op.TanOperator.Evaluate(expression, context),

                // logarithms & constants
                Expression.Operators.Ln => Op.LnOperator.Evaluate(expression, context),
                Expression.Operators.Ln2 => Op.Ln2Operator.Evaluate(expression, context),
                Expression.Operators.Log10 => Op.Log10Operator.Evaluate(expression, context),
                Expression.Operators.Log2 => Op.Log2Operator.Evaluate(expression, context),
                Expression.Operators.Pi => Op.PiOperator.Evaluate(expression, context),
                Expression.Operators.E => Op.EOperator.Evaluate(expression, context),

                // min/max & randomness
                Expression.Operators.Max => Op.MaxOperator.Evaluate(expression, context),
                Expression.Operators.Min => Op.MinOperator.Evaluate(expression, context),
                Expression.Operators.Random => Op.RandomOperator.Evaluate(expression, context),

                // geographic
                Expression.Operators.Distance => Op.DistanceOperator.Evaluate(expression, context),

                _ => throw new NotImplementedException($"Operator {expression.Operator} not supported yet.")
            };

            return new ExpressionValue(result);
        }

        // Recursively evaluate nested Expression or return primitive
        internal static object Evaluate(Expression expression, int operandIdx, ExpressionContext ctx)
        {
            var operand = expression.Operands[operandIdx];

            if (operand is not Expression expr) return operand;
            return Evaluate(expr, ctx);
        }

        // Try numeric, string, or bool equality
        internal static bool IsEqual(object a, object b)
        {
            if (IsNumber(a) && IsNumber(b)) return ToDouble(a) == ToDouble(b);

            if (a is string sa && b is string sb) return sa == sb;

            if (a is bool ba && b is bool bb) return ba == bb;

            // Fallback: reference or value-type equality
            return Equals(a, b);
        }

        // -1 if a<b, 0 if a==b, +1 if a>b
        internal static int Compare(object a, object b)
        {
            if (IsNumber(a) && IsNumber(b))
            {
                double da = ToDouble(a), db = ToDouble(b);
                return da.CompareTo(db);
            }

            if (a is string sa && b is string sb) return string.Compare(sa, sb, StringComparison.Ordinal);

            throw new InvalidOperationException(
                $"Cannot compare types {a?.GetType().Name} and {b?.GetType().Name}"
            );
        }

        internal static bool IsNumber(object o) =>
            o is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;

        private static double ToDouble(object o) => Convert.ToDouble(o);

        internal static bool AsBool(object o)
        {
            if (o is not bool b)
            {
                throw new InvalidOperationException($"Cannot convert {o?.GetType().Name} to bool");
            }

            return b;
        }

        public static double ToNumber(object value)
        {
            return ToDouble(IsNumber(value) ? value : value.ToString());
        }
    }
}