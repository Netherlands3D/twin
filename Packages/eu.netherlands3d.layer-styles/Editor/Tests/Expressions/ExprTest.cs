using System;
using NUnit.Framework;

namespace Netherlands3D.LayerStyles.Expressions
{
    [TestFixture]
    public class ExprTest
    {
        [Test]
        public void ExpressALiteralInteger()
        {
            Expr<int> e = 42;

            Assert.IsTrue(e.IsValue);
            Assert.IsFalse(e.IsExpression);
            Assert.AreEqual(Operators.Value, e.Operator);
            Assert.AreEqual(42, (int)e.Value);
            Assert.IsNull(e.Arguments);
        }

        [Test]
        public void ExpressALiteralString()
        {
            Expr<string> e = "hello";

            Assert.IsTrue(e.IsValue);
            Assert.AreEqual(Operators.Value, e.Operator);
            Assert.IsFalse(e.IsExpression);
            Assert.AreEqual("hello", (string)e.Value);
            Assert.IsNull(e.Arguments);
        }

        [Test]
        public void ExpressALiteralBoolean()
        {
            Expr<bool> e = true;

            Assert.IsTrue(e.IsValue);
            Assert.IsFalse(e.IsExpression);
            Assert.AreEqual(Operators.Value, e.Operator);
            Assert.AreEqual(true, (bool)e.Value);
            Assert.IsNull(e.Arguments);
        }

        [Test]
        public void ExpressALiteralFloat()
        {
            Expr<float> e = 3.14f;

            Assert.IsTrue(e.IsValue);
            Assert.IsFalse(e.IsExpression);
            Assert.AreEqual(Operators.Value, e.Operator);
            Assert.AreEqual(3.14f, (float)e.Value, 1e-6f);
            Assert.IsNull(e.Arguments);
        }

        [Test]
        public void ExpressALiteralDouble()
        {
            Expr<double> e = 2.71828d;

            Assert.IsTrue(e.IsValue);
            Assert.IsFalse(e.IsExpression);
            Assert.AreEqual(Operators.Value, e.Operator);
            Assert.AreEqual(2.71828d, (double)e.Value);
            Assert.IsNull(e.Arguments);
        }

        [Test]
        public void ExpressAGreaterComparison()
        {
            Expr<bool> cmp = Expr.GreaterThan(5, 10);

            Assert.IsFalse(cmp.IsValue);
            Assert.IsTrue(cmp.IsExpression);
            Assert.AreEqual(Operators.GreaterThan, cmp.Operator);

            Assert.IsNotNull(cmp.Arguments);
            Assert.AreEqual(2, cmp.Arguments.Length);
            Assert.AreEqual(5, (int)cmp.Arguments[0].Value);
            Assert.AreEqual(10, (int)cmp.Arguments[1].Value);
        }

        [Test]
        public void ExpressAnEqualityComparison()
        {
            Expr<bool> eq = Expr.EqualsTo(7, 8);

            Assert.AreEqual(Operators.EqualTo, eq.Operator);

            Assert.IsNotNull(eq.Arguments);
            Assert.AreEqual(2, eq.Arguments.Length);
            Assert.AreEqual(7, (int)eq.Arguments[0].Value);
            Assert.AreEqual(8, (int)eq.Arguments[1].Value);
        }

        [Test]
        public void ExpressAnEqualityComparisonOnStrings()
        {
            Expr<bool> eq = Expr.EqualsTo("hello", "world");

            Assert.AreEqual(Operators.EqualTo, eq.Operator);

            Assert.IsNotNull(eq.Arguments);
            Assert.AreEqual(2, eq.Arguments.Length);
            Assert.AreEqual("hello", (string)eq.Arguments[0].Value);
            Assert.AreEqual("world", (string)eq.Arguments[1].Value);
        }

        [Test]
        public void ExpressComparisonsWithAllNumericTypes()
        {
            int i = 3;
            float f = 4.0f;
            double d = 5.0d;

            Expr<bool> eq = Expr.EqualsTo(i, f);
            Expr<bool> gt = Expr.GreaterThan(i, f);
            Expr<bool> gte = Expr.GreaterThanOrEqual(f, d);
            Expr<bool> lt = Expr.LessThan(d, i);
            Expr<bool> lte = Expr.LessThanOrEqual(i, d);

            Assert.AreEqual(Operators.EqualTo, eq.Operator);
            Assert.AreEqual(Operators.GreaterThan, gt.Operator);
            Assert.AreEqual(Operators.GreaterThanOrEqual, gte.Operator);
            Assert.AreEqual(Operators.LessThan, lt.Operator);
            Assert.AreEqual(Operators.LessThanOrEqual, lte.Operator);
        }

        [Test]
        public void ExpressCreatingAColorFromItsRgbComponents()
        {
            Expr<int> r = 255;
            Expr<int> g = 128;
            Expr<int> b = 0;

            Expr<string> rgb = Expr.Rgb(r, g, b);

            Assert.AreEqual(Operators.Rgb, rgb.Operator);
            Assert.AreEqual(3, rgb.Arguments.Length);

            Assert.AreEqual(255, (int)rgb.Arguments[0].Value);
            Assert.AreEqual(128, (int)rgb.Arguments[1].Value);
            Assert.AreEqual(0, (int)rgb.Arguments[2].Value);
        }

        [Test]
        public void ExpressGettingAStringVariableFromTheContext()
        {
            Expr<string> nameExpr = "foo";
            Expr<IConvertible> getVariable = Expr.GetVariable(nameExpr);

            Assert.AreEqual(Operators.GetVariable, getVariable.Operator);
            Assert.AreEqual(1, getVariable.Arguments.Length);
            Assert.AreEqual("foo", (string)getVariable.Arguments[0].Value);
        }

        [Test]
        public void ExpressGettingAnIntegerVariableFromTheContext()
        {
            Expr<string> nameExpr = 5;
            Expr<IConvertible> getVariable = Expr.GetVariable(nameExpr);

            Assert.AreEqual(Operators.GetVariable, getVariable.Operator);
            Assert.AreEqual(1, getVariable.Arguments.Length);
            Assert.AreEqual(5, (int)getVariable.Arguments[0].Value);
        }

        [Test]
        public void ExampleOfNestedExpression()
        {
            /*
             Example for an expression that creates a Color based on its R, G and B values where
             R is the value from the variable temperature, G is a literal zero, and B equals the value from the
             variable temperature or 100, whichever is lower.

             See https://docs.mapbox.com/style-spec/reference/expressions/#data-expressions

             [
                "rgb",
                // red is higher when feature.properties.temperature is higher
                ["get", "temperature"],
                // green is always zero
                0,
                // blue is higher when feature.properties.temperature is lower
                ["-", 100, ["get", "temperature"]]
             ]
             */
            
            // Act: Get expression that will grab the variable with name "temperature"
            Expr<int> temperatureVariable = Expr.GetVariable("temperature");

            // Act: Make expressions for the R, G and B components of the color
            Expr<int> r = temperatureVariable;
            Expr<int> g = 0;
            Expr<int> b = Expr.Min(100, temperatureVariable);
            
            // Act: Make the final expression that will grab the color using the previous expressions as input
            Expr<string> rgbExpr = Expr.Rgb(r, g, b);

            // HINT: Can also be written in a single expression for increased readability
            // var rgbExpr = Expr.Rgb(
            //     Expr.GetVariable("temperature"), 
            //     0, 
            //     Expr.Min((Expr<int>)100, Expr.GetVariable("temperature"))
            // );

            // Assert: Top-level rgb expression
            Assert.AreEqual(Operators.Rgb, rgbExpr.Operator);
            Assert.AreEqual(3, rgbExpr.Arguments.Length);

            // Assert: Red channel == temperature variable
            Expr<int> redArg = rgbExpr.Arguments[0] as Expr<int>;
            Assert.IsNotNull(redArg);
            Assert.AreEqual(Operators.GetVariable, redArg.Operator);
            Assert.AreEqual("temperature", (string)redArg.Arguments[0].Value);

            // Assert: Green channel == literal 0
            Expr<int> greenArg = rgbExpr.Arguments[1] as Expr<int>;
            Assert.IsNotNull(greenArg);
            Assert.IsTrue(greenArg.IsValue);
            Assert.AreEqual(0, (int)greenArg.Value);

            // Assert: Blue channel == min expression
            Expr<int> blueArg = rgbExpr.Arguments[2] as Expr<int>;
            Assert.IsNotNull(blueArg);
            Assert.AreEqual(Operators.Min, blueArg.Operator);
            Assert.AreEqual(2, blueArg.Arguments.Length);

            // Assert: first operand of min expression == 100
            Assert.AreEqual(100, (int)blueArg.Arguments[0].Value);

            // Assert: second operand == temperature variable
            IExpression nestedGet = blueArg.Arguments[1];
            Assert.IsNotNull(nestedGet);
            Assert.AreEqual(Operators.GetVariable, nestedGet.Operator);
            Assert.AreEqual("temperature", (string)nestedGet.Arguments[0].Value);
        }
    }
}