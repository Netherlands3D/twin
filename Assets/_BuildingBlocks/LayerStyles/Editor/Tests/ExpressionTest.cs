using System;
using NUnit.Framework;
using UnityEngine;

namespace Netherlands3D.LayerStyles
{
    [TestFixture]
    public class ExprTests
    {
        [Test]
        public void ExpressALiteralInteger()
        {
            Expr<int> e = 42;

            Assert.IsTrue(e.IsLiteral);
            Assert.IsFalse(e.IsExpression);
            Assert.AreEqual(TypeOfExpression.Literal, e.Operator);
            Assert.AreEqual(42, e.Value);
            Assert.IsNull(e.Arguments);
        }

        [Test]
        public void ExpressALiteralString()
        {
            Expr<string> e = "hello";

            Assert.IsTrue(e.IsLiteral);
            Assert.AreEqual(TypeOfExpression.Literal, e.Operator);
            Assert.IsFalse(e.IsExpression);
            Assert.AreEqual("hello", e.Value);
            Assert.IsNull(e.Arguments);
        }

        [Test]
        public void ExpressALiteralBoolean()
        {
            Expr<bool> e = true;

            Assert.IsTrue(e.IsLiteral);
            Assert.IsFalse(e.IsExpression);
            Assert.AreEqual(TypeOfExpression.Literal, e.Operator);
            Assert.AreEqual(true, e.Value);
            Assert.IsNull(e.Arguments);
        }

        [Test]
        public void ExpressALiteralFloat()
        {
            Expr<float> e = 3.14f;

            Assert.IsTrue(e.IsLiteral);
            Assert.IsFalse(e.IsExpression);
            Assert.AreEqual(TypeOfExpression.Literal, e.Operator);
            Assert.AreEqual(3.14f, (float)e.Value, 1e-6f);
            Assert.IsNull(e.Arguments);
        }

        [Test]
        public void ExpressALiteralDouble()
        {
            Expr<double> e = 2.71828d;

            Assert.IsTrue(e.IsLiteral);
            Assert.IsFalse(e.IsExpression);
            Assert.AreEqual(TypeOfExpression.Literal, e.Operator);
            Assert.AreEqual(2.71828d, e.Value);
            Assert.IsNull(e.Arguments);
        }

        [Test]
        public void ExpressAGreaterComparison()
        {
            Expr<int> left = 5;
            Expr<int> right = 10;

            Expr<bool> cmp = Expr.Greater(left, right);

            Assert.IsFalse(cmp.IsLiteral);
            Assert.IsTrue(cmp.IsExpression);
            Assert.AreEqual(TypeOfExpression.Greater, cmp.Operator);

            Assert.IsNotNull(cmp.Arguments);
            Assert.AreEqual(2, cmp.Arguments.Length);
            Assert.AreEqual(5, cmp.Arguments[0].Value);
            Assert.AreEqual(10, cmp.Arguments[1].Value);
        }

        [Test]
        public void ExpressAnEqualityComparison()
        {
            Expr<int> a = 7;
            Expr<int> b = 8;

            Expr<bool> eq = Expr.Equals(a, b);

            Assert.AreEqual(TypeOfExpression.EqualTo, eq.Operator);

            Assert.IsNotNull(eq.Arguments);
            Assert.AreEqual(2, eq.Arguments.Length);
            Assert.AreEqual(7, eq.Arguments[0].Value);
            Assert.AreEqual(8, eq.Arguments[1].Value);
        }

        [Test]
        public void ExpressAnEqualityComparisonOnStrings()
        {
            Expr<string> a = "hello";
            Expr<string> b = "world";

            Expr<bool> eq = Expr.Equals(a, b);

            Assert.AreEqual(TypeOfExpression.EqualTo, eq.Operator);

            Assert.IsNotNull(eq.Arguments);
            Assert.AreEqual(2, eq.Arguments.Length);
            Assert.AreEqual("hello", eq.Arguments[0].Value);
            Assert.AreEqual("world", eq.Arguments[1].Value);
        }

        [Test]
        public void ExpressComparisonsWithAllNumericTypes()
        {
            Expr<int> i = 3;
            Expr<float> f = 4.0f;
            Expr<double> d = 5.0d;

            Expr<bool> eq = Expr.Equals(i, f);
            Expr<bool> gt = Expr.Greater(i, f);
            Expr<bool> gte = Expr.GreaterThan(f, d);
            Expr<bool> lt = Expr.Less(d, i);
            Expr<bool> lte = Expr.LessThan(i, d);

            Assert.AreEqual(TypeOfExpression.EqualTo, eq.Operator);
            Assert.AreEqual(TypeOfExpression.Greater, gt.Operator);
            Assert.AreEqual(TypeOfExpression.GreaterThan, gte.Operator);
            Assert.AreEqual(TypeOfExpression.Less, lt.Operator);
            Assert.AreEqual(TypeOfExpression.LessThan, lte.Operator);
        }

        [Test]
        public void ExpressCreatingAColorFromItsRgbComponents()
        {
            Expr<int> r = 255;
            Expr<int> g = 128;
            Expr<int> b = 0;

            Expr<Color> rgb = Expr.Rgb(r, g, b);

            Assert.AreEqual(TypeOfExpression.Rgb, rgb.Operator);
            Assert.AreEqual(3, rgb.Arguments.Length);

            Assert.AreEqual(255, rgb.Arguments[0].Value);
            Assert.AreEqual(128, rgb.Arguments[1].Value);
            Assert.AreEqual(0, rgb.Arguments[2].Value);
        }

        [Test]
        public void ExpressGettingAStringVariableFromTheContext()
        {
            Expr<string> nameExpr = "foo";
            Expr<IConvertible> getVariable = Expr.GetVariable(nameExpr);

            Assert.AreEqual(TypeOfExpression.GetVariable, getVariable.Operator);
            Assert.AreEqual(1, getVariable.Arguments.Length);
            Assert.AreEqual("foo", getVariable.Arguments[0].Value);
        }

        [Test]
        public void ExpressGettingAnIntegerVariableFromTheContext()
        {
            Expr<string> nameExpr = 5;
            Expr<IConvertible> getVariable = Expr.GetVariable(nameExpr);

            Assert.AreEqual(TypeOfExpression.GetVariable, getVariable.Operator);
            Assert.AreEqual(1, getVariable.Arguments.Length);
            Assert.AreEqual(5, getVariable.Arguments[0].Value);
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
            Expr<IConvertible> temperatureVariable = Expr.GetVariable("temperature");

            // Act: Make expressions for the R, G and B components of the color
            Expr<IConvertible> r = temperatureVariable;
            Expr<int> g = 0;
            Expr<IConvertible> b = Expr.Min((Expr<int>)100, temperatureVariable);
            
            // Act: Make the final expression that will grab the color using the previous expressions as input
            Expr<Color> rgbExpr = Expr.Rgb(r, g, b);

            // HINT: Can also be written in a single expression for increased readability
            // var rgbExpr = Expr.Rgb(
            //     Expr.GetVariable("temperature"), 
            //     0, 
            //     Expr.Min((Expr<int>)100, Expr.GetVariable("temperature"))
            // );

            // Assert: Top-level rgb expression
            Assert.AreEqual(TypeOfExpression.Rgb, rgbExpr.Operator);
            Assert.AreEqual(3, rgbExpr.Arguments.Length);

            // Assert: Red channel == temperature variable
            Expr<int> redArg = rgbExpr.Arguments[0] as Expr<int>;
            Assert.IsNotNull(redArg);
            Assert.AreEqual(TypeOfExpression.GetVariable, redArg.Operator);
            Assert.AreEqual("temperature", redArg.Arguments[0].Value);

            // Assert: Green channel == literal 0
            Expr<int> greenArg = rgbExpr.Arguments[1] as Expr<int>;
            Assert.IsNotNull(greenArg);
            Assert.IsTrue(greenArg.IsLiteral);
            Assert.AreEqual(0, greenArg.Value);

            // Assert: Blue channel == min expression
            Expr<int> blueArg = rgbExpr.Arguments[2] as Expr<int>;
            Assert.IsNotNull(blueArg);
            Assert.AreEqual(TypeOfExpression.Min, blueArg.Operator);
            Assert.AreEqual(2, blueArg.Arguments.Length);

            // Assert: first operand of min expression == 100
            Assert.AreEqual(100, blueArg.Arguments[0].Value);

            // Assert: second operand == temperature variable
            Expr<IConvertible> nestedGet = (Expr<IConvertible>)blueArg.Arguments[1];
            Assert.IsNotNull(nestedGet);
            Assert.AreEqual(TypeOfExpression.GetVariable, nestedGet.Operator);
            Assert.AreEqual("temperature", nestedGet.Arguments[0].Value);
        }
    }
}