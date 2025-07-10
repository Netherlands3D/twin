using System;
using System.Globalization;
using System.Linq;
using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.Expressions.OperatorHandlers
{
    public static class ArrayOperator
    {
        public const string Code = "array";

        // ["array", value]
        // ["array", type, value]
        // ["array", type, N, value]
        // https://docs.mapbox.com/style-spec/reference/expressions/#types-array
        public static object[] Evaluate(Expression expression, ExpressionContext context)
        {
            var operands = expression.Operands;
            if (operands.Length == 1)
            {
                var v = ExpressionEvaluator.Evaluate(expression, 0, context);
                if (v is object[] objectArray) return objectArray;
                if (v is Array anyArray) return BoxToObjectArray(anyArray);
                
                throw new InvalidOperationException(
                    $"\"array\" assertion failed: value is not an array but {v.GetType()}"
                );
            }

            var typeName = ExpressionEvaluator.Evaluate(expression, 0, context)?.ToString();
            if (typeName == null)
            {
                throw new InvalidOperationException("array: missing type string.");
            }

            object[] result = null;
            int valueIndex, indexLength = -1;
            switch (operands.Length)
            {
                case 2: valueIndex = 1; break;
                case 3:
                    // ["array", type, value]
                    valueIndex = 2;
                    indexLength = -1;
                    break;
                case 4:
                    // ["array", type, N, value]
                    indexLength = Convert.ToInt32(ExpressionEvaluator.Evaluate(expression, 1, context),
                        CultureInfo.InvariantCulture);
                    valueIndex = 3;
                    break;
                default:
                    throw new InvalidOperationException($"\"array\" takes 1–3 arguments, got {operands.Length}.");
            }

            var raw = ExpressionEvaluator.Evaluate(expression, valueIndex, context);
            if (raw is not object[] arrVal)
            {
                throw new InvalidOperationException($"\"array\" assertion failed: value is not an array.");
            }

            result = arrVal;

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
            var len = anyArray.Length;
            var boxed = new object[len];
            for (int i = 0; i < len; i++)
            {
                boxed[i] = anyArray.GetValue(i);
            }
            return boxed;
        }

        private static void ValidateArrayType(object[] arr, string typeName)
        {
            foreach (var e in arr)
            {
                bool ok = typeName switch
                {
                    "string" => e is string,
                    "number" => ExpressionEvaluator.IsNumber(e),
                    "boolean" => e is bool,
                    _ => throw new InvalidOperationException($"\"array\": unknown element type \"{typeName}\"")
                };
                if (!ok)
                    throw new InvalidOperationException(
                        $"\"array\" assertion failed: element {e} is not of type {typeName}.");
            }
        }
    }
}