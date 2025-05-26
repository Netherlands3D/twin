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

            var expressionValue = evaluator.Evaluate(e, context);
            
            Assert.IsInstanceOf<ExpressionValue>(expressionValue);
            Assert.AreEqual(value, (int)expressionValue);
        }

        [Test]
        public void EvaluateLiteralStringExpression()
        {
            const string value = "hello";
            Expr<string> e = value;

            var expressionValue = evaluator.Evaluate(e, context);
            
            Assert.IsInstanceOf<ExpressionValue>(expressionValue);
            Assert.AreEqual(value, (string)expressionValue);
        }

        [Test]
        public void EvaluateLiteralDoubleExpression()
        {
            const double value = 4.2d;
            Expr<double> e = value;

            var expressionValue = evaluator.Evaluate(e, context);

            Assert.IsInstanceOf<ExpressionValue>(expressionValue);
            Assert.AreEqual((double)expressionValue, value, 1e-6f);
        }

        [Test]
        public void EvaluateLiteralFloatExpression()
        {
            const float value = 4.2f;
            Expr<float> e = value;

            var expressionValue = evaluator.Evaluate(e, context);
            
            Assert.IsInstanceOf<ExpressionValue>(expressionValue);
            Assert.AreEqual((float)expressionValue, value, 1e-6f);
        }

        [Test]
        public void EvaluateLiteralBoolExpression()
        {
            const bool value = true;
            Expr<bool> e = value;

            var expressionValue = evaluator.Evaluate(e, context);
            
            Assert.IsInstanceOf<ExpressionValue>(expressionValue);
            Assert.AreEqual(value, (bool)expressionValue);
        }

        [Test]
        public void EvaluateLiteralArrayExpression()
        {
            ExpressionValue[] value = {};
            Expr<ExpressionValue[]> e = value;

            var expressionValues = (ExpressionValue[])evaluator.Evaluate(e, context);
            
            Assert.AreEqual(value, expressionValues);
        }

        [Test]
        public void EvaluateEqualsToExpression()
        {
            var match = Expr.EqualsTo(42, 42);
            var noMatch = Expr.EqualsTo(41, 42);

            var equalsValue = evaluator.Evaluate(match, context);
            var inequalValue = evaluator.Evaluate(noMatch, context);

            Assert.IsInstanceOf<ExpressionValue>(equalsValue);

            Assert.IsTrue((bool)equalsValue);
            Assert.IsFalse((bool)inequalValue);
        }

        [Test]
        public void EvaluateGreaterThanExpression()
        {
            var greater = Expr.GreaterThan(43, 42);
            var equals = Expr.GreaterThan(42, 42);
            var less = Expr.GreaterThan(42, 43);

            var greaterValue = evaluator.Evaluate(greater, context);
            var equalsValue = evaluator.Evaluate(equals, context);
            var lessThanValue = evaluator.Evaluate(less, context);

            Assert.IsInstanceOf<ExpressionValue>(greaterValue);

            Assert.IsTrue((bool)greaterValue);
            Assert.IsFalse((bool)equalsValue);
            Assert.IsFalse((bool)lessThanValue);
        }

        [Test]
        public void EvaluateGreaterThanOrEqualExpression()
        {
            var greater = Expr.GreaterThanOrEqual(43, 42);
            var equals = Expr.GreaterThanOrEqual(42, 42);
            var less = Expr.GreaterThanOrEqual(42, 43);

            var greaterValue = evaluator.Evaluate(greater, context);
            var equalsValue = evaluator.Evaluate(equals, context);
            var lessThanValue = evaluator.Evaluate(less, context);

            Assert.IsInstanceOf<ExpressionValue>(greaterValue);

            Assert.IsTrue((bool)greaterValue);
            Assert.IsTrue((bool)equalsValue);
            Assert.IsFalse((bool)lessThanValue);
        }

        [Test]
        public void EvaluateLessThanExpression()
        {
            var greater = Expr.LessThan(43, 42);
            var equals = Expr.LessThan(42, 42);
            var less = Expr.LessThan(42, 43);

            var greaterValue = evaluator.Evaluate(greater, context);
            var equalsValue = evaluator.Evaluate(equals, context);
            var lessThanValue = evaluator.Evaluate(less, context);

            Assert.IsInstanceOf<ExpressionValue>(greaterValue);

            Assert.IsFalse((bool)greaterValue);
            Assert.IsFalse((bool)equalsValue);
            Assert.IsTrue((bool)lessThanValue);
        }

        [Test]
        public void EvaluateLessThanOrEqualExpression()
        {
            var greater = Expr.LessThanOrEqual(43, 42);
            var equals = Expr.LessThanOrEqual(42, 42);
            var less = Expr.LessThanOrEqual(42, 43);

            var greaterValue = evaluator.Evaluate(greater, context);
            var equalsValue = evaluator.Evaluate(equals, context);
            var lessThanValue = evaluator.Evaluate(less, context);

            Assert.IsInstanceOf<ExpressionValue>(greaterValue);

            Assert.IsFalse((bool)greaterValue);
            Assert.IsTrue((bool)equalsValue);
            Assert.IsTrue((bool)lessThanValue);
        }

        [Test]
        public void EvaluateGetVariableExpression()
        {
            context.Feature.Attributes.Add("temperature", "100");
            Expr<ExpressionValue> getVariableExpression = Expr.GetVariable("temperature");

            var expressionValue = evaluator.Evaluate(getVariableExpression, context);
            
            Assert.IsInstanceOf<ExpressionValue>(expressionValue);
            Assert.AreEqual("100", (string)expressionValue);
        }

        [Test]
        public void EvaluateGetVariableExpressionWithUnknownVariableReturnsNull()
        {
            Expr<ExpressionValue> getVariableExpression = Expr.GetVariable("unknownVariable");

            string expressionValue = evaluator.Evaluate(getVariableExpression, context);
            
            Assert.IsNull(expressionValue);
        }

        [Test]
        public void EvaluateMinExpression()
        {
            Expr<int> minExpression = Expr.Min(100, 60);

            var expressionValue = evaluator.Evaluate(minExpression, context);
            
            Assert.IsInstanceOf<ExpressionValue>(expressionValue);
            Assert.AreEqual(60, (int)expressionValue);
        }

        [Test]
        public void EvaluateRgbExpression()
        {
            Expr<string> rgbExpression = Expr.Rgb(100, 60, 10);

            var expressionValue = evaluator.Evaluate(rgbExpression, context);

            Assert.IsInstanceOf<ExpressionValue>(expressionValue);
            Assert.AreEqual("643C0A", (string)expressionValue);
        }

        [Test]
        public void EvaluateExampleOfNestedExpression()
        {
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