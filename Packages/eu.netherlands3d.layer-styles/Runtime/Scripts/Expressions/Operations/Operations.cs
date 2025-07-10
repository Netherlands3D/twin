using System;
using System.Globalization;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    internal static class Operations
    {
        internal static void GuardNumberOfOperands(string operation, Expression expression, int expected)
        {
            int actual = expression.Operands.Length;

            if (actual != expected)
            {
                throw new InvalidOperationException(
                    $"\"{operation}\" requires exactly {expected} operands, got {actual}."
                );
            }
        }

        internal static void GuardAtLeastNumberOfOperands(string code, Expression expression, int atLeast)
        {
            int count = expression.Operands.Length;
            if (count >= atLeast) return;

            throw new InvalidOperationException($"\"{code}\" requires at least {atLeast} operands, got {count}.");
        }
        
        internal static double GetNumericOperand(
            string operation,
            string name, 
            Expression expression,
            int index,
            ExpressionContext context
        ) {
            var raw = ExpressionEvaluator.Evaluate(expression, index, context);
            if (!ExpressionEvaluator.IsNumber(raw))
            {
                throw new InvalidOperationException(
                    $"\"{operation}\" requires numeric operand for {name}, got {raw?.GetType().Name}."
                );
            }

            return Convert.ToDouble(raw, CultureInfo.InvariantCulture);
        }
        
        internal static void GuardInRange(string code, string name, double value, double min, double max)
        {
            if (!(value < min) && !(value > max)) return;

            throw new InvalidOperationException($"\"{code}\": {name} {value} out of range {min}–{max}.");
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

        internal static double ToDouble(object o) => Convert.ToDouble(o);

        internal static bool AsBool(object o)
        {
            if (o is not bool b)
            {
                throw new InvalidOperationException($"Cannot convert {o?.GetType().Name} to bool");
            }

            return b;
        }

        internal static double ToNumber(object value)
        {
            return ToDouble(IsNumber(value) ? value : value.ToString());
        }
    }
}