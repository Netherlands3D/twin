using System;
using System.Collections;
using UnityEngine;

namespace Netherlands3D.SerializableGisExpressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>in</c> expression operator, which returns
    /// <c>true</c> if the first operand is found within the second operand.
    /// </summary>
    /// <remarks>
    /// - When both operands are <c>string</c>, this performs a substring check
    ///   (<c>haystack.Contains(needle)</c>).
    /// - When the second operand is an <see cref="IEnumerable"/>, this checks
    ///   membership (<c>sequence.Any(item => IsEqual(item, needle))</c>).
    /// </remarks>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#in">
    ///   Mapbox “in” expression reference
    /// </seealso>
    public static class InOperation
    {
        /// <summary>The Mapbox operator string for <c>in</c>.</summary>
        public const string Code = "in";

        /// <summary>
        /// Evaluates the “in” expression by checking whether the first operand
        /// (the “needle”) appears within the second operand (the “haystack”).
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operands to inspect.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing any feature or runtime data.</param>
        /// <returns>
        /// <c>true</c> if the needle is contained in the haystack; otherwise <c>false</c>.
        /// </returns>
        public static bool Evaluate(Expression expression, ExpressionContext context)
        {
            // Extract the two operands
            var needle = ExpressionEvaluator.Evaluate(expression, 0, context);
            var haystack = ExpressionEvaluator.Evaluate(expression, 1, context);

            // If either is an expression value, convert them to string for the next bit
            if (needle is ExpressionValue needleValue && needleValue.IsString()) needle = needleValue.ToString();
            if (haystack is ExpressionValue haystackValue && haystackValue.IsString()) haystack = haystackValue.ToString();
            
            // Case 1: both are strings → substring
            if (needle is string s && haystack is string str)
            {
                return str.Contains(s, StringComparison.OrdinalIgnoreCase);
            }

            // Case 2: haystack is a sequence → membership
            if (haystack is IEnumerable sequence)
            {
                foreach (var element in sequence)
                {
                    if (Operations.IsEqual(element, needle)) return true;
                }

                return false;
            }

            // Other types are not supported
            return false;
        }
    }
}