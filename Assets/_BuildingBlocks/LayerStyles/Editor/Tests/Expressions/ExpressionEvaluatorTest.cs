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

            Assert.AreEqual(evaluator.Evaluate(e, context), value);
        }

        [Test]
        public void EvaluateLiteralStringExpression()
        {
            const string value = "hello";
            Expr<string> e = value;

            Assert.AreEqual(evaluator.Evaluate(e, context), value);
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

            Assert.AreEqual(evaluator.Evaluate(e, context), value);
        }

        [Test]
        public void EvaluateLiteralArrayExpression()
        {
            Array value = new Array();
            Expr<Array> e = value;

            Assert.AreEqual(evaluator.Evaluate(e, context), value);
        }

        [Test]
        public void EvaluateEqualsToExpression()
        {
            var match = Expr.EqualsTo((Expr<int>)42, (Expr<int>)42);
            var noMatch = Expr.EqualsTo((Expr<int>)41, (Expr<int>)42);

            Assert.IsTrue((bool)evaluator.Evaluate(match, context));
            Assert.IsFalse((bool)evaluator.Evaluate(noMatch, context));
        }

        [Test]
        public void EvaluateGreaterThanExpression()
        {
            var greater = Expr.GreaterThan((Expr<int>)43, (Expr<int>)42);
            var equals = Expr.GreaterThan((Expr<int>)42, (Expr<int>)42);
            var less = Expr.GreaterThan((Expr<int>)42, (Expr<int>)43);

            Assert.IsTrue((bool)evaluator.Evaluate(greater, context));
            Assert.IsFalse((bool)evaluator.Evaluate(equals, context));
            Assert.IsFalse((bool)evaluator.Evaluate(less, context));
        }

        [Test]
        public void EvaluateGreaterThanOrEqualExpression()
        {
            var greater = Expr.GreaterThanOrEqual((Expr<int>)43, (Expr<int>)42);
            var equals = Expr.GreaterThanOrEqual((Expr<int>)42, (Expr<int>)42);
            var less = Expr.GreaterThanOrEqual((Expr<int>)42, (Expr<int>)43);

            Assert.IsTrue((bool)evaluator.Evaluate(greater, context));
            Assert.IsTrue((bool)evaluator.Evaluate(equals, context));
            Assert.IsFalse((bool)evaluator.Evaluate(less, context));
        }

        [Test]
        public void EvaluateLessThanExpression()
        {
            var greater = Expr.LessThan((Expr<int>)43, (Expr<int>)42);
            var equals = Expr.LessThan((Expr<int>)42, (Expr<int>)42);
            var less = Expr.LessThan((Expr<int>)42, (Expr<int>)43);

            Assert.IsFalse((bool)evaluator.Evaluate(greater, context));
            Assert.IsFalse((bool)evaluator.Evaluate(equals, context));
            Assert.IsTrue((bool)evaluator.Evaluate(less, context));
        }

        [Test]
        public void EvaluateLessThanOrEqualExpression()
        {
            var greater = Expr.LessThanOrEqual((Expr<int>)43, (Expr<int>)42);
            var equals = Expr.LessThanOrEqual((Expr<int>)42, (Expr<int>)42);
            var less = Expr.LessThanOrEqual((Expr<int>)42, (Expr<int>)43);

            Assert.IsFalse((bool)evaluator.Evaluate(greater, context));
            Assert.IsTrue((bool)evaluator.Evaluate(equals, context));
            Assert.IsTrue((bool)evaluator.Evaluate(less, context));
        }

        [Test]
        public void EvaluateGetVariableExpression()
        {
            context.Feature.Attributes.Add("temperature", "100");
            Expr<IConvertible> getVariableExpression = Expr.GetVariable("temperature");
            
            Assert.AreEqual(evaluator.Evaluate(getVariableExpression, context), "100");
        }

        [Test]
        public void EvaluateMinExpression()
        {
            Expr<int> minExpression = Expr.Min((Expr<int>)100, (Expr<int>)60);

            Assert.AreEqual(60, evaluator.Evaluate(minExpression, context));
        }

        [Test]
        public void EvaluateRgbExpression()
        {
            Expr<string> rgbExpression = Expr.Rgb(100, 60, 10);

            Assert.AreEqual("643C0A", evaluator.Evaluate(rgbExpression, context));
        }

        [Test]
        public void EvaluateExampleOfNestedExpression()
        {
            var rgbExpr = Expr.Rgb(
            Expr.GetVariable("temperature"), 
            0, 
            Expr.Min((Expr<int>)100, Expr.GetVariable("temperature"))
            );

            var layerFeature = LayerFeature.Create("string");
            layerFeature.Attributes.Add("temperature", "100");
            
            evaluator.Evaluate(rgbExpr, new ExpressionContext(layerFeature));
        }
    }
}