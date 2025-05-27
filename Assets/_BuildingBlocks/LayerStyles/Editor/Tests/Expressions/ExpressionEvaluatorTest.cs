using System;
using Netherlands3D.Twin.Layers;
using NUnit.Framework;

namespace Netherlands3D.LayerStyles.Expressions
{
    [TestFixture]
    public class ExpressionEvaluatorTest
    {
        private ExpressionEvaluator evaluator;
        private ExpressionContext context;

        [SetUp]
        public void Setup()
        {
            evaluator = new ExpressionEvaluator();
            context = new ExpressionContext(LayerFeature.Create("string"));
        }
        
        [Test]
        public void EvaluateLiteralIntegerExpression()
        {
            const int value = 42;
            Expr<int> e = value;

            Assert.AreEqual(value, (int)evaluator.Evaluate(e, context));
        }

        [Test]
        public void EvaluateLiteralStringExpression()
        {
            const string value = "hello";
            Expr<string> e = value;

            Assert.AreEqual(value, (string)evaluator.Evaluate(e, context));
        }

        [Test]
        public void EvaluateLiteralDoubleExpression()
        {
            const double value = 4.2d;
            Expr<double> e = value;

            Assert.AreEqual((double)evaluator.Evaluate(e, context), value, 1e-6f);
        }

        [Test]
        public void EvaluateLiteralFloatExpression()
        {
            const float value = 4.2f;
            Expr<float> e = value;

            Assert.AreEqual((float)evaluator.Evaluate(e, context), value, 1e-6f);
        }

        [Test]
        public void EvaluateLiteralBoolExpression()
        {
            const bool value = true;
            Expr<bool> e = value;

            Assert.AreEqual(value, (bool)evaluator.Evaluate(e, context));
        }

        [Test]
        public void EvaluateLiteralArrayExpression()
        {
            ExpressionValue[] value = {};
            Expr<ExpressionValue[]> e = value;

            Assert.AreEqual(value, (ExpressionValue[])evaluator.Evaluate(e, context));
        }

        [Test]
        public void EvaluateEqualsToExpression()
        {
            var match = Expr.EqualsTo(42, 42);
            var noMatch = Expr.EqualsTo(41, 42);

            Assert.IsTrue((bool)evaluator.Evaluate(match, context));
            Assert.IsFalse((bool)evaluator.Evaluate(noMatch, context));
        }

        [Test]
        public void EvaluateGreaterThanExpression()
        {
            var greater = Expr.GreaterThan(43, 42);
            var equals = Expr.GreaterThan(42, 42);
            var less = Expr.GreaterThan(42, 43);

            Assert.IsTrue((bool)evaluator.Evaluate(greater, context));
            Assert.IsFalse((bool)evaluator.Evaluate(equals, context));
            Assert.IsFalse((bool)evaluator.Evaluate(less, context));
        }

        [Test]
        public void EvaluateGreaterThanOrEqualExpression()
        {
            var greater = Expr.GreaterThanOrEqual(43, 42);
            var equals = Expr.GreaterThanOrEqual(42, 42);
            var less = Expr.GreaterThanOrEqual(42, 43);

            Assert.IsTrue((bool)evaluator.Evaluate(greater, context));
            Assert.IsTrue((bool)evaluator.Evaluate(equals, context));
            Assert.IsFalse((bool)evaluator.Evaluate(less, context));
        }

        [Test]
        public void EvaluateLessThanExpression()
        {
            var greater = Expr.LessThan(43, 42);
            var equals = Expr.LessThan(42, 42);
            var less = Expr.LessThan(42, 43);

            Assert.IsFalse((bool)evaluator.Evaluate(greater, context));
            Assert.IsFalse((bool)evaluator.Evaluate(equals, context));
            Assert.IsTrue((bool)evaluator.Evaluate(less, context));
        }

        [Test]
        public void EvaluateLessThanOrEqualExpression()
        {
            var greater = Expr.LessThanOrEqual(43, 42);
            var equals = Expr.LessThanOrEqual(42, 42);
            var less = Expr.LessThanOrEqual(42, 43);

            Assert.IsFalse((bool)evaluator.Evaluate(greater, context));
            Assert.IsTrue((bool)evaluator.Evaluate(equals, context));
            Assert.IsTrue((bool)evaluator.Evaluate(less, context));
        }

        [Test]
        public void EvaluateGetVariableExpression()
        {
            // TODO: Add another test that will check what happens if you pass an invalid type of attribute
            context.Feature.Attributes.Add("temperature", "100");
            Expr<ExpressionValue> getVariableExpression = Expr.GetVariable("temperature");
            
            Assert.AreEqual("100", (string)evaluator.Evaluate(getVariableExpression, context));
        }

        [Test]
        public void EvaluateMinExpression()
        {
            Expr<int> minExpression = Expr.Min(100, 60);

            Assert.AreEqual(60, (int)evaluator.Evaluate(minExpression, context));
        }

        [Test]
        public void EvaluateRgbExpression()
        {
            Expr<string> rgbExpression = Expr.Rgb(100, 60, 10);

            Assert.AreEqual("643C0A", (string)evaluator.Evaluate(rgbExpression, context));
        }

        [Test]
        public void EvaluateExampleOfNestedExpression()
        {
            // TODO: Test fails because Expr.Min evaluates to null instead of a correct value
            var rgbExpr = Expr.Rgb(
            Expr.GetVariable("temperature"), 
            0, 
            Expr.Min(100, Expr.GetVariable("temperature"))
            );

            var layerFeature = LayerFeature.Create("string");
            layerFeature.Attributes.Add("temperature", "100");
            
            evaluator.Evaluate(rgbExpr, new ExpressionContext(layerFeature));
        }
    }
}