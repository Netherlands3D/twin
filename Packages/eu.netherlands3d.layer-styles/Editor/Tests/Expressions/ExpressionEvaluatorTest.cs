using System;
using System.Collections.Generic;
using Netherlands3D.LayerStyles.Expressions;
using Netherlands3D.Twin.Layers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Netherlands3D.LayerStyles.ExpressionEngine
{
    [TestFixture]
    public class ExpressionEvaluatorTest
    {
        private ExpressionContext context;

        [SetUp]
        public void Setup()
        {
            context = new ExpressionContext(LayerFeature.Create("string"));
        }

        [Test]
        public void EvaluateLiteralIntegerExpression()
        {
            const int value = 42;

            var expressionValue = ExpressionEvaluator.Evaluate(value, context);

            Assert.IsInstanceOf<ExpressionValue>(expressionValue);
            Assert.AreEqual(value, (int)expressionValue);
        }

        [Test]
        public void EvaluateLiteralStringExpression()
        {
            const string value = "hello";

            var expressionValue = ExpressionEvaluator.Evaluate(value, context);

            Assert.IsInstanceOf<ExpressionValue>(expressionValue);
            Assert.AreEqual(value, (string)expressionValue);
        }

        [Test]
        public void EvaluateLiteralDoubleExpression()
        {
            const double value = 4.2d;

            var expressionValue = ExpressionEvaluator.Evaluate(value, context);

            Assert.IsInstanceOf<ExpressionValue>(expressionValue);
            Assert.AreEqual(expressionValue, value, 1e-6f);
        }

        [Test]
        public void EvaluateLiteralFloatExpression()
        {
            const float value = 4.2f;

            var expressionValue = ExpressionEvaluator.Evaluate(value, context);

            Assert.IsInstanceOf<ExpressionValue>(expressionValue);
            Assert.AreEqual(expressionValue, value, 1e-6f);
        }

        [Test]
        public void EvaluateLiteralBoolExpression()
        {
            const bool value = true;

            var expressionValue = ExpressionEvaluator.Evaluate(value, context);

            Assert.IsInstanceOf<ExpressionValue>(expressionValue);
            Assert.AreEqual(value, (bool)expressionValue);
        }

        [Test]
        public void EvaluateLiteralArrayExpression()
        {
            object[] value = { };

            ExpressionValue expressionValue =
                ExpressionEvaluator.Evaluate(value, context);

            Assert.AreEqual(value, (object[])expressionValue);
        }

        [Test]
        public void EvaluateNotExpression()
        {
            var exprTrue = Expression.Not(false);
            var exprFalse = Expression.Not(true);

            var valTrue = ExpressionEvaluator.Evaluate(exprTrue, context);
            var valFalse = ExpressionEvaluator.Evaluate(exprFalse, context);

            Assert.IsInstanceOf<ExpressionValue>(valTrue);
            Assert.IsTrue(valTrue);
            Assert.IsFalse(valFalse);
        }

        [Test]
        public void EvaluateNotEqualExpression()
        {
            var expr1 = Expression.NotEqual(1, 2);
            var expr2 = Expression.NotEqual(2, 2);

            var val1 = ExpressionEvaluator.Evaluate(expr1, context);
            var val2 = ExpressionEvaluator.Evaluate(expr2, context);

            Assert.IsInstanceOf<ExpressionValue>(val1);
            Assert.IsTrue(val1);
            Assert.IsFalse(val2);
        }

        [Test]
        public void EvaluateEqualToExpression()
        {
            var match = Expression.EqualTo(42, 42);
            var noMatch = Expression.EqualTo(41, 42);

            var equalsValue = ExpressionEvaluator.Evaluate(match, context);
            var inequalValue = ExpressionEvaluator.Evaluate(noMatch, context);

            Assert.IsInstanceOf<ExpressionValue>(equalsValue);

            Assert.IsTrue(equalsValue);
            Assert.IsFalse(inequalValue);
        }

        [Test]
        public void EvaluateGreaterThanExpression()
        {
            var greater = Expression.GreaterThan(43, 42);
            var equals = Expression.GreaterThan(42, 42);
            var less = Expression.GreaterThan(42, 43);

            var greaterValue = ExpressionEvaluator.Evaluate(greater, context);
            var equalsValue = ExpressionEvaluator.Evaluate(equals, context);
            var lessThanValue = ExpressionEvaluator.Evaluate(less, context);

            Assert.IsInstanceOf<ExpressionValue>(greaterValue);

            Assert.IsTrue(greaterValue);
            Assert.IsFalse(equalsValue);
            Assert.IsFalse(lessThanValue);
        }

        [Test]
        public void EvaluateGreaterThanOrEqualExpression()
        {
            var greater = Expression.GreaterThanOrEqual(43, 42);
            var equals = Expression.GreaterThanOrEqual(42, 42);
            var less = Expression.GreaterThanOrEqual(42, 43);

            var greaterValue = ExpressionEvaluator.Evaluate(greater, context);
            var equalsValue = ExpressionEvaluator.Evaluate(equals, context);
            var lessThanValue = ExpressionEvaluator.Evaluate(less, context);

            Assert.IsInstanceOf<ExpressionValue>(greaterValue);

            Assert.IsTrue(greaterValue);
            Assert.IsTrue(equalsValue);
            Assert.IsFalse(lessThanValue);
        }

        [Test]
        public void EvaluateLessThanExpression()
        {
            var greater = Expression.LessThan(43, 42);
            var equals = Expression.LessThan(42, 42);
            var less = Expression.LessThan(42, 43);

            var greaterValue = ExpressionEvaluator.Evaluate(greater, context);
            var equalsValue = ExpressionEvaluator.Evaluate(equals, context);
            var lessThanValue = ExpressionEvaluator.Evaluate(less, context);

            Assert.IsInstanceOf<ExpressionValue>(greaterValue);

            Assert.IsFalse(greaterValue);
            Assert.IsFalse(equalsValue);
            Assert.IsTrue(lessThanValue);
        }

        [Test]
        public void EvaluateLessThanOrEqualExpression()
        {
            var greater = Expression.LessThanOrEqual(43, 42);
            var equals = Expression.LessThanOrEqual(42, 42);
            var less = Expression.LessThanOrEqual(42, 43);

            var greaterValue = ExpressionEvaluator.Evaluate(greater, context);
            var equalsValue = ExpressionEvaluator.Evaluate(equals, context);
            var lessThanValue = ExpressionEvaluator.Evaluate(less, context);

            Assert.IsInstanceOf<ExpressionValue>(greaterValue);

            Assert.IsFalse(greaterValue);
            Assert.IsTrue(equalsValue);
            Assert.IsTrue(lessThanValue);
        }

        [Test]
        public void EvaluateAllExpression()
        {
            var allTrue = Expression.All(true, true, true);
            var oneFalse = Expression.All(true, false, true);

            var vAllTrue = ExpressionEvaluator.Evaluate(allTrue, context);
            var vOneFalse = ExpressionEvaluator.Evaluate(oneFalse, context);

            Assert.IsInstanceOf<ExpressionValue>(vAllTrue);
            Assert.IsTrue(vAllTrue);
            Assert.IsFalse(vOneFalse);
        }

        [Test]
        public void EvaluateAnyExpression()
        {
            var anyTrue = Expression.Any(false, false, true);
            var allFalse = Expression.Any(false, false, false);

            var vAnyTrue = ExpressionEvaluator.Evaluate(anyTrue, context);
            var vAllFalse = ExpressionEvaluator.Evaluate(allFalse, context);

            Assert.IsInstanceOf<ExpressionValue>(vAnyTrue);
            Assert.IsTrue(vAnyTrue);
            Assert.IsFalse(vAllFalse);
        }

        [Test]
        public void EvaluateArrayExpression()
        {
            object[] arr = { 1, 2, 3 };
            var expr = Expression.Array(arr);

            var val = ExpressionEvaluator.Evaluate(expr, context);

            Assert.IsInstanceOf<ExpressionValue>(val);
            CollectionAssert.AreEqual(arr, (object[])val);
        }

        [Test]
        public void EvaluateBooleanExpression()
        {
            // first operand not bool, second is true
            var expr = Expression.Boolean("nope", true);
            var val = ExpressionEvaluator.Evaluate(expr, context);

            Assert.IsInstanceOf<ExpressionValue>(val);
            Assert.IsTrue(val);
        }

        [Test]
        public void EvaluateToBooleanExpression()
        {
            Assert.IsFalse(ExpressionEvaluator.Evaluate(Expression.ToBoolean(null), context));
            Assert.IsFalse(ExpressionEvaluator.Evaluate(Expression.ToBoolean(""), context));
            Assert.IsTrue(ExpressionEvaluator.Evaluate(Expression.ToBoolean("hello"), context));
            Assert.IsTrue(ExpressionEvaluator.Evaluate(Expression.ToBoolean(1), context));
            Assert.IsFalse(ExpressionEvaluator.Evaluate(Expression.ToBoolean(0), context));
        }

        [Test]
        public void EvaluateToNumberExpression()
        {
            Assert.AreEqual(0.0, ExpressionEvaluator.Evaluate(Expression.ToNumber(null), context), 1e-6);
            Assert.AreEqual(1.0, ExpressionEvaluator.Evaluate(Expression.ToNumber(true), context), 1e-6);
            Assert.AreEqual(0.0, ExpressionEvaluator.Evaluate(Expression.ToNumber(false), context), 1e-6);
            Assert.AreEqual(2.5, ExpressionEvaluator.Evaluate(Expression.ToNumber("2.5"), context), 1e-6);
        }

        [Test]
        public void EvaluateToStringExpression()
        {
            Assert.AreEqual("", (string)ExpressionEvaluator.Evaluate(Expression.ToString(null), context));
            Assert.AreEqual("true", (string)ExpressionEvaluator.Evaluate(Expression.ToString(true), context));
            Assert.AreEqual("3.14", (string)ExpressionEvaluator.Evaluate(Expression.ToString(3.14), context));
        }

        [Test]
        public void EvaluateTypeOfExpression()
        {
            Assert.AreEqual("number", (string)ExpressionEvaluator.Evaluate(Expression.TypeOf(123), context));
            Assert.AreEqual("string", (string)ExpressionEvaluator.Evaluate(Expression.TypeOf("hi"), context));
            Assert.AreEqual("boolean", (string)ExpressionEvaluator.Evaluate(Expression.TypeOf(true), context));
            Assert.AreEqual("array",
                (string)ExpressionEvaluator.Evaluate(Expression.TypeOf(new object[] { 1, 2 }), context));
            Assert.AreEqual("null", (string)ExpressionEvaluator.Evaluate(Expression.TypeOf(null), context));
            Assert.AreEqual("object",
                (string)ExpressionEvaluator.Evaluate(Expression.TypeOf(new Dictionary<string, string>()), context));
        }

        [Test]
        public void EvaluateLiteralExpression()
        {
            var expr = Expression.Literal(1, "two", false);
            var val = ExpressionEvaluator.Evaluate(expr, context);

            Assert.IsInstanceOf<ExpressionValue>(val);
            CollectionAssert.AreEqual(new object[] { 1, "two", false }, (object[])val);
        }

        [Test]
        public void EvaluateNumberAssertionExpression()
        {
            var expr1 = Expression.Number("nope", 5, 3);
            var expr2 = Expression.Number("nope", "nope", 7.5);

            Assert.AreEqual(5.0, ExpressionEvaluator.Evaluate(expr1, context), 1e-6);
            Assert.AreEqual(7.5, ExpressionEvaluator.Evaluate(expr2, context), 1e-6);
        }

        [Test]
        public void EvaluateStringAssertionExpression()
        {
            var expr = Expression.String("first", 123, "second");
            var val = ExpressionEvaluator.Evaluate(expr, context);

            Assert.IsInstanceOf<ExpressionValue>(val);
            Assert.AreEqual("first", (string)val);
        }

        [Test]
        public void EvaluateObjectAssertionExpression()
        {
            var dict = new Dictionary<string, string> { { "k", "v" } };
            var expr = Expression.Object("nope", dict, 1);
            var val = ExpressionEvaluator.Evaluate(expr, context);

            Assert.IsInstanceOf<ExpressionValue>(val);
            Assert.AreSame(dict, val);
        }

        [Test]
        public void EvaluateNumberFormatExpression()
        {
            // format 1.2345 → "1.23" with 2 fraction digits in en-US
            var opts = JObject.FromObject(new { locale = "en-US", minFractionDigits = 2, maxFractionDigits = 2 });
            var expr = Expression.NumberFormat(1.2345, opts);
            var val = (string)ExpressionEvaluator.Evaluate(expr, context);

            Assert.AreEqual("1.23", val);
        }

        [Test]
        public void EvaluateGetVariableExpression()
        {
            context.Feature.Attributes.Add("temperature", "100");
            var getVariableExpression = Expression.Get("temperature");

            var expressionValue = ExpressionEvaluator.Evaluate(getVariableExpression, context);

            Assert.IsInstanceOf<ExpressionValue>(expressionValue);
            Assert.AreEqual("100", (string)expressionValue);
        }

        [Test]
        public void EvaluateGetVariableExpressionWithUnknownVariableReturnsNull()
        {
            var getVariableExpression = Expression.Get("unknownVariable");

            string expressionValue = ExpressionEvaluator.Evaluate(getVariableExpression, context);

            Assert.IsNull(expressionValue);
        }

        [Test]
        public void EvaluateAddSubtractMultiplyDivideModuloPowerExpressions()
        {
            Assert.AreEqual(6.0, ExpressionEvaluator.Evaluate(Expression.Add(1, 2, 3), context), 1e-6);
            Assert.AreEqual(5.0, ExpressionEvaluator.Evaluate(Expression.Subtract(10, 3, 2), context), 1e-6);
            Assert.AreEqual(24.0, ExpressionEvaluator.Evaluate(Expression.Multiply(2, 3, 4), context), 1e-6);
            Assert.AreEqual(2.0, ExpressionEvaluator.Evaluate(Expression.Divide(8, 2, 2), context), 1e-6);
            Assert.AreEqual(1.0, ExpressionEvaluator.Evaluate(Expression.Modulo(10, 3), context), 1e-6);
            Assert.AreEqual(8.0, ExpressionEvaluator.Evaluate(Expression.Power(2, 3), context), 1e-6);
        }

        [Test]
        public void EvaluateUnaryMathExpressions()
        {
            Assert.AreEqual(5.0, ExpressionEvaluator.Evaluate(Expression.Abs(-5), context), 1e-6);
            Assert.AreEqual(5.0, ExpressionEvaluator.Evaluate(Expression.Ceil(4.2), context), 1e-6);
            Assert.AreEqual(4.0, ExpressionEvaluator.Evaluate(Expression.Floor(4.8), context), 1e-6);
            Assert.AreEqual(4.0, ExpressionEvaluator.Evaluate(Expression.Round(4.4), context), 1e-6);
            Assert.AreEqual(3.0, ExpressionEvaluator.Evaluate(Expression.Sqrt(9), context), 1e-6);
        }

        [Test]
        public void EvaluateTrigonometryExpressions()
        {
            Assert.AreEqual(0.0, ExpressionEvaluator.Evaluate(Expression.Sin(0), context), 1e-6);
            Assert.AreEqual(0.0, ExpressionEvaluator.Evaluate(Expression.Tan(0), context), 1e-6);
            Assert.AreEqual(Math.Acos(0.5), ExpressionEvaluator.Evaluate(Expression.Acos(0.5), context), 1e-6);
            Assert.AreEqual(Math.Asin(0.5), ExpressionEvaluator.Evaluate(Expression.Asin(0.5), context), 1e-6);
            Assert.AreEqual(Math.Atan(1.0), ExpressionEvaluator.Evaluate(Expression.Atan(1), context), 1e-6);
        }

        [Test]
        public void EvaluateLogAndConstantExpressions()
        {
            Assert.AreEqual(Math.Log(10), ExpressionEvaluator.Evaluate(Expression.Ln(10), context), 1e-6);
            Assert.AreEqual(Math.Log(2.0), ExpressionEvaluator.Evaluate(Expression.Ln2(), context), 1e-6);
            Assert.AreEqual(3.0, ExpressionEvaluator.Evaluate(Expression.Log10(1000), context), 1e-6);
            Assert.AreEqual(3.0, ExpressionEvaluator.Evaluate(Expression.Log2(8), context), 1e-6);
            Assert.AreEqual(Math.PI, ExpressionEvaluator.Evaluate(Expression.Pi(), context), 1e-6);
            Assert.AreEqual(Math.E, ExpressionEvaluator.Evaluate(Expression.E(), context), 1e-6);
        }

        [Test]
        public void EvaluateMaxMinRandomExpressions()
        {
            Assert.AreEqual(8.0, ExpressionEvaluator.Evaluate(Expression.Max(5, 8, 3), context), 1e-6);
            Assert.AreEqual(3.0, ExpressionEvaluator.Evaluate(Expression.Min(5, 3, 8), context), 1e-6);

            double randVal = ExpressionEvaluator.Evaluate(Expression.Random(), context);
            Assert.GreaterOrEqual(randVal, 0.0);
            Assert.LessOrEqual(randVal, 1.0);
        }

        [Test]
        public void EvaluateRgbExpression()
        {
            var rgbExpression = Expression.Rgb(100, 60, 10);

            var expressionValue = ExpressionEvaluator.Evaluate(rgbExpression, context);

            Assert.IsInstanceOf<ExpressionValue>(expressionValue);
            Assert.AreEqual("643C0A", ColorUtility.ToHtmlStringRGB(expressionValue));
        }

        [Test]
        public void EvaluateDistanceExpression()
        {
            object[] a = { 0.0, 0.0 };
            object[] b = { 0.0, 1.0 };
            var expr = Expression.Distance(a, b);

            var val = (double)ExpressionEvaluator.Evaluate(expr, context);
            // ≈ 111319 meters per degree of latitude
            Assert.AreEqual(111319.5, val, 1.0);
        }

        [Test]
        public void EvaluateHslHslaRgbRgbaAndConversions()
        {
            var red = (Color) ExpressionEvaluator.Evaluate(Expression.Hsl(0,100,50), context);
            Assert.AreEqual("FF0000", ColorUtility.ToHtmlStringRGB(red));

            var semiBlue = (Color) ExpressionEvaluator.Evaluate(Expression.Hsla(240,100,50,0.5), context);
            Assert.AreEqual("0000FF", ColorUtility.ToHtmlStringRGB(semiBlue));
            Assert.AreEqual(0.5f, semiBlue.a, 1e-6);

            var rgb = (Color) ExpressionEvaluator.Evaluate(Expression.Rgb(10, 20, 30), context);
            Assert.AreEqual("0A141E", ColorUtility.ToHtmlStringRGB(rgb));

            var rgba = (Color) ExpressionEvaluator.Evaluate(Expression.Rgba(10, 20, 30, 0.25), context);
            Assert.AreEqual("0A141E", ColorUtility.ToHtmlStringRGB(rgba));
            Assert.AreEqual(0.25f, rgba.a, 1e-6);

            var toHsla = (object[])ExpressionEvaluator.Evaluate(Expression.ToHsla(rgba), context);
            Assert.AreEqual(4, toHsla.Length);
            Assert.AreEqual(210.0, (double)toHsla[0], 1e-6);   // hue ≈ 210°
            Assert.AreEqual(50.0,  (double)toHsla[1], 1e-6);   // sat 50%
            Assert.AreEqual(11.76, (double)toHsla[2], 1e-2);   // light ≈ 11.76%
            Assert.AreEqual(0.25,  (double)toHsla[3], 1e-6);   // alpha

            var toRgba = (object[])ExpressionEvaluator.Evaluate(Expression.ToRgba(rgba), context);
            Assert.AreEqual(4, toRgba.Length);
            Assert.AreEqual(10.0,  (double)toRgba[0], 1e-6);
            Assert.AreEqual(20.0,  (double)toRgba[1], 1e-6);
            Assert.AreEqual(30.0,  (double)toRgba[2], 1e-6);
            Assert.AreEqual(0.25,  (double)toRgba[3], 1e-6);
        }
    
        [Test]
        public void EvaluateExampleOfNestedExpression()
        {
            var rgbExpr = Expression.Rgb(
                Expression.Get("temperature"),
                0,
                Expression.Min(100, Expression.Get("temperature"))
            );

            var layerFeature = LayerFeature.Create("string");
            layerFeature.Attributes.Add("temperature", "100");

            ExpressionEvaluator.Evaluate(rgbExpr, new ExpressionContext(layerFeature));
        }
    }
}