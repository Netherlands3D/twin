using System;
using System.Globalization;
using UnityEngine;

namespace Netherlands3D.SerializableGisExpressions.Operations
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
            
            if (raw is ExpressionValue ev) return ev; 
            
            if (!IsNumber(raw))
            {
                throw new InvalidOperationException(
                    $"\"{operation}\" requires numeric operand for {name}, got {raw?.GetType().Name}."
                );
            }

            return ToDouble(raw);
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

        internal static bool IsNumber(object o)
        {
            return o is short or ushort or int or uint or long or ulong or float or double;
        }

        internal static double ToDouble(object o)
        {
            if (o is ExpressionValue ev) return ev; 
            
            return Convert.ToDouble(o);
        }

        internal static bool AsBool(object o)
        {
            if (o is ExpressionValue ev) return ev; 
            
            if (o is not bool b)
            {
                throw new InvalidOperationException($"Cannot convert {o?.GetType().Name} to bool");
            }

            return b;
        }

        internal static double ToNumber(object value)
        {
            if (value is ExpressionValue ev) return ev; 
            
            return ToDouble(IsNumber(value) ? value : value.ToString());
        }
        
        /// <summary>
        /// Parses the specified operand as a <see cref="Color"/>, accepting either a
        /// <see cref="Color"/> or a CSS string literal.
        /// </summary>
        internal static Color GetColorOperand(
            string code,
            Expression expression,
            int index,
            ExpressionContext context)
        {
            object raw = ExpressionEvaluator.Evaluate(expression, index, context);

            if (raw is ExpressionValue ev) return ev; 
            if (raw is Color direct) return direct;
            if (raw is string css && ColorUtility.TryParseHtmlString(css, out Color parsed))
            {
                return parsed;
            }

            throw new InvalidOperationException(
                $"\"{code}\" requires a color input at operand {index}, got {raw?.GetType().Name}."
            );
        }

        /// <summary>
        /// Converts an RGB <see cref="Color"/> (components 0–1) into HSLA components
        /// (hue 0–360, saturation 0–1, lightness 0–1).
        /// </summary>
        internal static (double h, double s, double l) ConvertRgbToHsla(Color color)
        {
            // Using the same algorithm as Mapbox spec, normalized to [0,1]
            float r = color.r, g = color.g, b = color.b;
            float max = Mathf.Max(r, Mathf.Max(g, b));
            float min = Mathf.Min(r, Mathf.Min(g, b));
            float delta = max - min;
            float lightness = (max + min) / 2f;

            float hue;
            float saturation;

            if (delta == 0f)
            {
                hue = 0f;
                saturation = 0f;
            }
            else
            {
                saturation = delta / (1f - Math.Abs(2f * lightness - 1f));

                if (Mathf.Approximately(max, r))
                {
                    hue = 60f * (((g - b) / delta) % 6f);
                }
                else if (Mathf.Approximately(max, g))
                {
                    hue = 60f * (((b - r) / delta) + 2f);
                }
                else
                {
                    hue = 60f * (((r - g) / delta) + 4f);
                }

                if (hue < 0f)
                {
                    hue += 360f;
                }
            }

            return (hue, saturation, lightness);
        }        
    }
}