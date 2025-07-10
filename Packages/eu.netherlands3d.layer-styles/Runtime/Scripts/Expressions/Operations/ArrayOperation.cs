using System;
using System.Globalization;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>array</c> expression operator, which
    /// either returns its operand as an array or enforces a type and/or length check:
    /// <code>
    /// ["array", value]
    /// ["array", type, value]
    /// ["array", type, length, value]
    /// </code>
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#types-array">
    ///   Mapbox “array” expression reference
    /// </seealso>
    public static class ArrayOperation
    {
        /// <summary>The Mapbox operator string for “array”.</summary>
        public const string Code = "array";

        /// <summary>
        /// Evaluates the <c>array</c> expression, performing any required type or length assertions.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operands encode the assertion.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing any runtime data needed.</param>
        /// <returns>The resulting <see cref="object[]"/> if all checks pass.</returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if the operand is not an array, if the type string is missing or unknown,
        ///   or if a fixed-length assertion fails.
        /// </exception>
        public static object[] Evaluate(Expression expression, ExpressionContext context)
        {
            var operands = expression.Operands;
            var operandCount = operands.Length;

            // Single-operand: assert it was already an array.
            if (operandCount == 1)
            {
                var v = ExpressionEvaluator.Evaluate(expression, 0, context);
                if (v is object[] objectArray) return objectArray;
                if (v is Array anyArray) return BoxToObjectArray(anyArray);

                throw new InvalidOperationException(
                    $"\"{Code}\" assertion failed: value is not an array but {v?.GetType().Name}"
                );
            }

            // Multi-operand: first must be a type string.
            var typeName = ExpressionEvaluator
                .Evaluate(expression, 0, context)?
                .ToString();

            if (typeName == null)
            {
                throw new InvalidOperationException($"{Code}: missing type string.");
            }

            int valueIndex, indexLength = -1;
            switch (operands.Length)
            {
                case 2: valueIndex = 1; break;
                // ["array", type, value]
                case 3: valueIndex = 2; break;
                // ["array", type, N, value]
                case 4:
                    indexLength = Convert.ToInt32(
                        ExpressionEvaluator.Evaluate(expression, 1, context),
                        CultureInfo.InvariantCulture
                    );
                    valueIndex = 3;
                    break;
                default:
                    throw new InvalidOperationException($"\"array\" takes 1–3 arguments, got {operands.Length}.");
            }

            var raw = ExpressionEvaluator.Evaluate(expression, valueIndex, context);
            if (raw is not object[] result)
            {
                throw new InvalidOperationException($"\"array\" assertion failed: value is not an array.");
            }

            if (indexLength >= 0 && result.Length != indexLength)
            {
                throw new InvalidOperationException(
                    $"\"array\" assertion failed: expected length {indexLength} but got {result.Length}."
                );
            }

            ValidateArrayType(result, typeName);

            return result;
        }

        private static object[] BoxToObjectArray(Array anyArray)
        {
            var length = anyArray.Length;
            var boxed = new object[length];
            for (int i = 0; i < length; i++)
            {
                boxed[i] = anyArray.GetValue(i);
            }

            return boxed;
        }

        private static void ValidateArrayType(object[] array, string typeName)
        {
            foreach (var element in array)
            {
                bool ok = typeName switch
                {
                    "string" => element is string,
                    "number" => ExpressionEvaluator.IsNumber(element),
                    "boolean" => element is bool,
                    _ => throw new InvalidOperationException($"\"array\": unknown element type \"{typeName}\"")
                };
                
                if (!ok)
                {
                    throw new InvalidOperationException(
                        $"\"array\" assertion failed: element {element} is not of type {typeName}."
                    );
                }
            }
        }
    }
}