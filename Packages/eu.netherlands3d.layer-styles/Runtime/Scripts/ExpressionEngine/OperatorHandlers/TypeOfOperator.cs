using Netherlands3D.LayerStyles.Expressions;
using UnityEngine;

namespace Netherlands3D.LayerStyles.ExpressionEngine.OperatorHandlers
{
    public static class TypeOfOperator
    {
        public const string Code = "typeof";

        public static string Evaluate(Expression expression, ExpressionContext context)
        {
            var o = ExpressionEvaluator.Evaluate(expression, 0, context);
            if (ExpressionEvaluator.IsNumber(o)) return "number";

            return o switch
            {
                bool => "boolean",
                string => "string",
                Color => "color",
                object[] => "array",
                null => "null",
                _ => "object"
            };
        }
    }
}