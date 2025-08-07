using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.SerializableGisExpressions.Operations;

namespace Netherlands3D.SerializableGisExpressions
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


        static ExpressionEvaluator()
        {
            InitializeOperations();
        }

        public static Dictionary<Type, IOperation> operationMap = new Dictionary<Type, IOperation>();

        public static void InitializeOperations()
        {
            var operationType = typeof(IOperation);

            var allOperationTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try { return assembly.GetTypes(); }
                    catch { return Array.Empty<Type>(); } // in case of reflection errors
                })
                .Where(t => operationType.IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null);

            foreach (var type in allOperationTypes)
            {
                var instance = (IOperation)Activator.CreateInstance(type);
                operationMap[type] = instance;
            }
        }

        public static ExpressionValue Evaluate(Expression expression, ExpressionContext context = null)
        {
            var result = operationMap[expression.Operator.GetType()].EvaluateHello(expression, context);


            //var result = expression.Operator switch
            //{
                // https://docs.mapbox.com/style-spec/reference/expressions/#types
                //Expression.Operators.Array => ArrayOperation.Evaluate(expression, context),
                //Expression.Operators.Boolean => BooleanOperation.Evaluate(expression, context),
                //Expression.Operators.Literal => LiteralOperation.Evaluate(expression),
                //Expression.Operators.Number => NumberOperation.Evaluate(expression, context),
                //Expression.Operators.NumberFormat => NumberFormatOperation.Evaluate(expression, context),
                //Expression.Operators.Object => ObjectOperation.Evaluate(expression, context),
                //Expression.Operators.String => StringOperation.Evaluate(expression, context),
                //Expression.Operators.ToBoolean => ToBooleanOperation.Evaluate(expression, context),
                //Expression.Operators.ToColor => ToColorOperation.Evaluate(expression, context),
                //Expression.Operators.ToNumber => ToNumberOperation.Evaluate(expression, context),
                //Expression.Operators.ToString => ToStringOperation.Evaluate(expression, context),
                //Expression.Operators.TypeOf => TypeOfOperation.Evaluate(expression, context),

                //// https://docs.mapbox.com/style-spec/reference/expressions/#lookup
                //Expression.Operators.Get => GetOperation.Evaluate(expression, context),
                //Expression.Operators.In => InOperation.Evaluate(expression, context),

                //// https://docs.mapbox.com/style-spec/reference/expressions/#decision
                //Expression.Operators.Not => NotOperation.Evaluate(expression, context),
                //Expression.Operators.NotEqual => NotEqualOperation.Evaluate(expression, context),
                //Expression.Operators.LessThan => LessThanOperation.Evaluate(expression, context),
                //Expression.Operators.LessThanOrEqual => LessThanOrEqualOperation.Evaluate(expression, context),
                //Expression.Operators.EqualTo => EqualOperation.Evaluate(expression, context),
                //Expression.Operators.GreaterThan => GreaterThanOperation.Evaluate(expression, context),
                //Expression.Operators.GreaterThanOrEqual => GreaterThanOrEqualOperation.Evaluate(expression,
                //    context),
                //Expression.Operators.All => AllOperation.Evaluate(expression, context),
                //Expression.Operators.Any => AnyOperation.Evaluate(expression, context),

                //// https://docs.mapbox.com/style-spec/reference/expressions/#color
                //Expression.Operators.Hsl => HslOperation.Evaluate(expression, context),
                //Expression.Operators.Hsla => HslaOperation.Evaluate(expression, context),
                //Expression.Operators.Rgb => RgbOperation.Evaluate(expression, context),
                //Expression.Operators.Rgba => RgbaOperation.Evaluate(expression, context),
                //Expression.Operators.ToHsla => ToHslaOperation.Evaluate(expression, context),
                //Expression.Operators.ToRgba => ToRgbaOperation.Evaluate(expression, context),

                //// https://docs.mapbox.com/style-spec/reference/expressions/#math
                //// arithmetic
                //Expression.Operators.Add => AddOperation.Evaluate(expression, context),
                //Expression.Operators.Subtract => SubtractOperation.Evaluate(expression, context),
                //Expression.Operators.Multiply => MultiplyOperation.Evaluate(expression, context),
                //Expression.Operators.Divide => DivideOperation.Evaluate(expression, context),
                //Expression.Operators.Modulo => ModuloOperation.Evaluate(expression, context),
                //Expression.Operators.Power => PowerOperation.Evaluate(expression, context),

                //// unary math
                //Expression.Operators.Abs => AbsOperation.Evaluate(expression, context),
                //Expression.Operators.Ceil => CeilOperation.Evaluate(expression, context),
                //Expression.Operators.Floor => FloorOperation.Evaluate(expression, context),
                //Expression.Operators.Round => RoundOperation.Evaluate(expression, context),
                //Expression.Operators.Sqrt => SqrtOperation.Evaluate(expression, context),

                //// trigonometry
                //Expression.Operators.Acos => AcosOperation.Evaluate(expression, context),
                //Expression.Operators.Asin => AsinOperation.Evaluate(expression, context),
                //Expression.Operators.Atan => AtanOperation.Evaluate(expression, context),
                //Expression.Operators.Cos => CosOperation.Evaluate(expression, context),
                //Expression.Operators.Sin => SinOperation.Evaluate(expression, context),
                //Expression.Operators.Tan => TanOperation.Evaluate(expression, context),

                //// logarithms & constants
                //Expression.Operators.Ln => LnOperation.Evaluate(expression, context),
                //Expression.Operators.Ln2 => Ln2Operation.Evaluate(expression),
                //Expression.Operators.Log10 => Log10Operation.Evaluate(expression, context),
                //Expression.Operators.Log2 => Log2Operation.Evaluate(expression, context),
                //Expression.Operators.Pi => PiOperation.Evaluate(expression),
                //Expression.Operators.E => EOperation.Evaluate(),

                //// min/max & randomness
                //Expression.Operators.Max => MaxOperation.Evaluate(expression, context),
                //Expression.Operators.Min => MinOperation.Evaluate(expression, context),
                //Expression.Operators.Random => RandomOperation.Evaluate(expression, context),

            //    _ => throw new NotImplementedException($"Operator {expression.Operator} not supported yet.")
            //};

            return new ExpressionValue(result);
        }

        // Recursively evaluate nested Expression or return primitive
        internal static object Evaluate(Expression expression, int operandIdx, ExpressionContext ctx)
        {
            var operand = expression.Operands[operandIdx];

            if (operand is not Expression expr) return operand;

            return Evaluate(expr, ctx);
        }
    }
}