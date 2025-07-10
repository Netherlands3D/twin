using System;
using Netherlands3D.LayerStyles.Expressions.OperatorHandlers;

namespace Netherlands3D.LayerStyles.Expressions
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
                Expression.Operators.Array => ArrayOperator.Evaluate(expression, context),
                Expression.Operators.Boolean => BooleanOperator.Evaluate(expression, context),
                Expression.Operators.Literal => LiteralOperator.Evaluate(expression, context),
                Expression.Operators.Number => NumberOperator.Evaluate(expression, context),
                Expression.Operators.NumberFormat => NumberFormatOperator.Evaluate(expression, context),
                Expression.Operators.Object => ObjectOperator.Evaluate(expression, context),
                Expression.Operators.String => StringOperator.Evaluate(expression, context),
                Expression.Operators.ToBoolean => ToBooleanOperator.Evaluate(expression, context),
                Expression.Operators.ToColor => ToColorOperator.Evaluate(expression, context),
                Expression.Operators.ToNumber => ToNumberOperator.Evaluate(expression, context),
                Expression.Operators.ToString => ToStringOperator.Evaluate(expression, context),
                Expression.Operators.TypeOf => TypeOfOperator.Evaluate(expression, context),
                
                // https://docs.mapbox.com/style-spec/reference/expressions/#lookup
                Expression.Operators.Get => GetOperator.Evaluate(expression, context),

                // https://docs.mapbox.com/style-spec/reference/expressions/#decision
                Expression.Operators.Not => NotOperator.Evaluate(expression, context),
                Expression.Operators.NotEqual => NotEqualOperator.Evaluate(expression, context),
                Expression.Operators.LessThan => LessThanOperator.Evaluate(expression, context),
                Expression.Operators.LessThanOrEqual => LessThanOrEqualOperator.Evaluate(expression, context),
                Expression.Operators.EqualTo => EqualOperator.Evaluate(expression, context),
                Expression.Operators.GreaterThan => GreaterThanOperator.Evaluate(expression, context),
                Expression.Operators.GreaterThanOrEqual => GreaterThanOrEqualOperator.Evaluate(expression,
                    context),
                Expression.Operators.All => AllOperator.Evaluate(expression, context),
                Expression.Operators.Any => AnyOperator.Evaluate(expression, context),
                
                // https://docs.mapbox.com/style-spec/reference/expressions/#color
                Expression.Operators.Hsl => HslOperator.Evaluate(expression, context),
                Expression.Operators.Hsla => HslaOperator.Evaluate(expression, context),
                Expression.Operators.Rgb => RgbOperator.Evaluate(expression, context),
                Expression.Operators.Rgba => RgbaOperator.Evaluate(expression, context),
                Expression.Operators.ToHsla => ToHslaOperator.Evaluate(expression, context),
                Expression.Operators.ToRgba => ToRgbaOperator.Evaluate(expression, context),
                
                // https://docs.mapbox.com/style-spec/reference/expressions/#math
                // arithmetic
                Expression.Operators.Add => AddOperator.Evaluate(expression, context),
                Expression.Operators.Subtract => SubtractOperator.Evaluate(expression, context),
                Expression.Operators.Multiply => MultiplyOperator.Evaluate(expression, context),
                Expression.Operators.Divide => DivideOperator.Evaluate(expression, context),
                Expression.Operators.Modulo => ModuloOperator.Evaluate(expression, context),
                Expression.Operators.Power => PowerOperator.Evaluate(expression, context),

                // unary math
                Expression.Operators.Abs => AbsOperator.Evaluate(expression, context),
                Expression.Operators.Ceil => CeilOperator.Evaluate(expression, context),
                Expression.Operators.Floor => FloorOperator.Evaluate(expression, context),
                Expression.Operators.Round => RoundOperator.Evaluate(expression, context),
                Expression.Operators.Sqrt => SqrtOperator.Evaluate(expression, context),

                // trigonometry
                Expression.Operators.Acos => AcosOperator.Evaluate(expression, context),
                Expression.Operators.Asin => AsinOperator.Evaluate(expression, context),
                Expression.Operators.Atan => AtanOperator.Evaluate(expression, context),
                Expression.Operators.Cos => CosOperator.Evaluate(expression, context),
                Expression.Operators.Sin => SinOperator.Evaluate(expression, context),
                Expression.Operators.Tan => TanOperator.Evaluate(expression, context),

                // logarithms & constants
                Expression.Operators.Ln => LnOperator.Evaluate(expression, context),
                Expression.Operators.Ln2 => Ln2Operator.Evaluate(expression, context),
                Expression.Operators.Log10 => Log10Operator.Evaluate(expression, context),
                Expression.Operators.Log2 => Log2Operator.Evaluate(expression, context),
                Expression.Operators.Pi => PiOperator.Evaluate(expression, context),
                Expression.Operators.E => EOperator.Evaluate(expression, context),

                // min/max & randomness
                Expression.Operators.Max => MaxOperator.Evaluate(expression, context),
                Expression.Operators.Min => MinOperator.Evaluate(expression, context),
                Expression.Operators.Random => RandomOperator.Evaluate(expression, context),

                // geographic
                Expression.Operators.Distance => DistanceOperator.Evaluate(expression, context),

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
            o is short or ushort or int or uint or long or ulong or float or double;

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