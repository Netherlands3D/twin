using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("eu.netherlands3d.serializable-gis-expressions.editor.tests")]
namespace Netherlands3D.SerializableGisExpressions.Operations
{
    /// <summary>
    /// Provides common guard, comparison, and conversion helpers for expression evaluation operations.
    /// </summary>
    internal static class Operations
    {
        #region Guards
        /// <summary>
        /// Ensures the expression has exactly the expected number of operands.
        /// </summary>
        /// <param name="operation">The name or code of the operation being evaluated.</param>
        /// <param name="expression">The expression instance to inspect.</param>
        /// <param name="expected">The exact number of operands required.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the expression's operand count does not match <paramref name="expected"/>.
        /// </exception>
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

        /// <summary>
        /// Ensures the expression has at least the specified minimum number of operands.
        /// </summary>
        /// <param name="code">The name or code of the operation being evaluated.</param>
        /// <param name="expression">The expression instance to inspect.</param>
        /// <param name="atLeast">The minimum number of operands required.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the expression's operand count is less than <paramref name="atLeast"/>.
        /// </exception>
        internal static void GuardAtLeastNumberOfOperands(string code, Expression expression, int atLeast)
        {
            int count = expression.Operands.Length;
            if (count >= atLeast) return;

            throw new InvalidOperationException($"\"{code}\" requires at least {atLeast} operands, got {count}.");
        }
        
        /// <summary>
        /// Ensures a numeric value lies within the specified inclusive range.
        /// </summary>
        /// <param name="code">The name or code of the operation being evaluated.</param>
        /// <param name="name">The friendly name of the value for error messaging.</param>
        /// <param name="value">The numeric value to validate.</param>
        /// <param name="min">The inclusive minimum allowed value.</param>
        /// <param name="max">The inclusive maximum allowed value.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="value"/> is outside the range [<paramref name="min"/>, <paramref name="max"/>].
        /// </exception>
        internal static void GuardInRange(string code, string name, double value, double min, double max)
        {
            if (value < min || value > max)
            {
                throw new InvalidOperationException($"\"{code}\": {name} {value} out of range {min}–{max}.");
            }
        }
        #endregion
        
        #region Conversions and comparisons
        /// <summary>
        /// Compares two objects for equality, handling <see cref="ExpressionValue"/>,
        /// numbers (with tolerance), strings, and booleans.
        /// </summary>
        /// <param name="a">The first operand to compare.</param>
        /// <param name="b">The second operand to compare.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="a"/> and <paramref name="b"/> are equal; otherwise <c>false</c>.
        /// </returns>
        internal static bool IsEqual(object a, object b)
        {
            // 1. If either is an ExpressionValue, wrap both and use its deep == logic
            if (a is ExpressionValue || b is ExpressionValue)
            {
                var left  = a is ExpressionValue evA ? evA : new ExpressionValue(a);
                var right = b is ExpressionValue evB ? evB : new ExpressionValue(b);

                return left.Equals(right);
            }

            // 2. Quick wins for null or exact same reference
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;

            // 3. Numeric tolerance
            if (IsNumber(a) && IsNumber(b)) return Math.Abs(ToDouble(a) - ToDouble(b)) < 1e-6d;

            // 4. Primitive strict equals
            if (a is string sa && b is string sb) return sa == sb;
            if (a is bool ba && b is bool bb) return ba == bb;

            // 5. Final fallback
            return Equals(a, b);
        }

        /// <summary>
        /// Compares two objects, returning a sign indication: -1 if <paramref name="a"/> is less,
        /// 0 if equal, +1 if greater. Supports <see cref="ExpressionValue"/>, numbers, strings, and booleans.
        /// </summary>
        /// <param name="a">The first operand.</param>
        /// <param name="b">The second operand.</param>
        /// <returns>
        /// -1 if a is less than b;
        /// 0 if they are equal;
        /// +1 if a is greater.
        /// </returns>
        /// <seealso cref="IsEqual"/>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the types of <paramref name="a"/> and <paramref name="b"/> cannot be compared.
        /// </exception>
        internal static int Compare(object a, object b)
        {
            // 1. If either is an ExpressionValue, unwrap both and compare their inner Values
            if (a is ExpressionValue || b is ExpressionValue)
            {
                var left = a is ExpressionValue evA ? evA : new ExpressionValue(a);
                var right = b is ExpressionValue evB ? evB : new ExpressionValue(b);
                return Compare(left.Value, right.Value);
            }

            // 2. Quick reference and null checks
            if (ReferenceEquals(a, b)) return 0;
            if (a is null) return -1;
            if (b is null) return 1;

            // 3. Numeric comparison
            if (IsNumber(a) && IsNumber(b))
            {
                double da = ToDouble(a), db = ToDouble(b);
                return da.CompareTo(db);
            }

            // 4. String comparison
            if (a is string sa && b is string sb) return string.Compare(sa, sb, StringComparison.Ordinal);

            // 5. Boolean comparison
            if (a is bool ba && b is bool bb) return ba.CompareTo(bb);

            // 6. Everything else is invalid
            throw new InvalidOperationException(
                $"Cannot compare types {a.GetType().Name} and {b.GetType().Name}"
            );
        }

        /// <summary>
        /// Determines whether an object is a numeric type (integer or floating point).
        /// </summary>
        /// <param name="o">The object to inspect.</param>
        /// <returns><c>true</c> if <paramref name="o"/> is a numeric value; otherwise <c>false</c>.</returns>
        internal static bool IsNumber(object o)
        {
            return o is short or ushort or int or uint or long or ulong or float or double;
        }

        /// <summary>
        /// Converts an object or <see cref="ExpressionValue"/> to a <see cref="double"/>.
        /// </summary>
        /// <param name="o">The object to convert.</param>
        /// <returns>The converted double value.</returns>
        internal static double ToDouble(object o)
        {
            if (o is ExpressionValue ev) return ev; 
            
            return Convert.ToDouble(o, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts an object or <see cref="ExpressionValue"/> to a <see cref="bool"/>,
        /// throwing if it cannot be converted.
        /// </summary>
        /// <param name="o">The object to convert.</param>
        /// <returns>The converted boolean value.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="o"/> is not a boolean or <see cref="ExpressionValue"/> wrapping a boolean.
        /// </exception>
        internal static bool AsBool(object o)
        {
            if (o is ExpressionValue ev) return ev; 
            
            if (o is not bool b)
            {
                throw new InvalidOperationException($"Cannot convert {o?.GetType().Name} to bool");
            }

            return b;
        }
        #endregion

        #region Operands retrieval and conversions
        /// <summary>
        /// Evaluates the operand at the given index and converts it to a <see cref="double"/>,
        /// enforcing that it must be numeric.
        /// </summary>
        /// <param name="operation">The name or code of the operation being evaluated.</param>
        /// <param name="name">The friendly name of the operand for error messaging.</param>
        /// <param name="expression">The expression instance containing the operand.</param>
        /// <param name="index">The zero-based index of the operand to evaluate.</param>
        /// <param name="context">The evaluation context.</param>
        /// <returns>The operand value converted to <see cref="double"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the evaluated operand is not numeric.
        /// </exception>
        internal static double GetOperandAsNumber(
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

        /// <summary>
        /// Parses the specified operand as a <see cref="Color"/>, accepting either a
        /// <see cref="Color"/> or a CSS string literal (e.g. "#RRGGBB").
        /// </summary>
        /// <param name="code">The name or code of the operation being evaluated.</param>
        /// <param name="expression">The expression instance containing the operand.</param>
        /// <param name="index">The zero-based index of the operand to evaluate.</param>
        /// <param name="context">The evaluation context.</param>
        /// <returns>The parsed <see cref="Color"/> value.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the operand cannot be converted to a <see cref="Color"/>.
        /// </exception>
        internal static Color GetOperandAsColor(
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
        #endregion
        
        #region Value conversions
        /// <summary>
        /// Converts an RGB <see cref="Color"/> (components 0–1) into HSLA components
        /// (hue 0–360, saturation 0–1, lightness 0–1).
        /// </summary>
        /// <param name="color">The RGB <see cref="Color"/> to convert.</param>
        /// <returns>
        /// A tuple with <c>h</c> (0–360), <c>s</c> (0–1), and <c>l</c> (0–1).
        /// </returns>
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
        #endregion
    }
}