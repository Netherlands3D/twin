using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;

namespace Netherlands3D.LayerStyles.Expressions
{
    [TestFixture]
    public class ExprJsonConverterTests
    {
        private JsonSerializerSettings jsonSerializerSettings;

        [SetUp]
        public void SetUp()
        {
            jsonSerializerSettings = new JsonSerializerSettings();
            jsonSerializerSettings.Converters.Add(new ExprJsonConverter());
        }

        [Test]
        public void SerializingALiteralIntegerProducesJsonNumber()
        {
            Expr<int> lit = 42;
            
            string json = JsonConvert.SerializeObject(lit, jsonSerializerSettings);
            
            Assert.AreEqual("42", json);
        }

        [Test]
        public void DeserializingJsonNumberReturnsLiteralIntegerExpression()
        {
            const string json = "42";
            
            var expr = JsonConvert.DeserializeObject<Expr<int>>(json, jsonSerializerSettings);
            
            Assert.IsInstanceOf<Expr>(expr);
            Assert.IsTrue(expr.IsValue);
            Assert.AreEqual(42, (int)expr); // implicit → int
        }

        [Test]
        public void SerializingALiteralDoubleProducesJsonFloat()
        {
            Expr<double> lit = 4.2d;
            
            string json = JsonConvert.SerializeObject(lit, jsonSerializerSettings);
            
            Assert.AreEqual("4.2", json);
        }

        [Test]
        public void DeserializingJsonFloatReturnsLiteralDoubleExpression()
        {
            const string json = "4.2";
            
            var expr = JsonConvert.DeserializeObject<Expr<double>>(json, jsonSerializerSettings);
            
            Assert.IsInstanceOf<Expr>(expr);
            Assert.IsTrue(expr.IsValue);
            Assert.AreEqual(4.2d, (double)expr); // implicit → double
        }

        [Test]
        public void SerializingALiteralBooleanProducesJsonBoolean()
        {
            Expr<bool> lit = true;
            
            string json = JsonConvert.SerializeObject(lit, jsonSerializerSettings);
            
            Assert.AreEqual("true", json);
        }

        [Test]
        public void DeserializingJsonBooleanReturnsLiteralBooleanExpression()
        {
            const string json = "true";
            
            var expr = JsonConvert.DeserializeObject<Expr<bool>>(json, jsonSerializerSettings);
            
            Assert.IsInstanceOf<Expr>(expr);
            Assert.IsTrue(expr.IsValue);
            Assert.AreEqual(true, (bool)expr); // implicit → bool
        }

        [Test]
        public void SerializingALiteralStringProducesJsonString()
        {
            Expr<string> lit = "hello";
            
            string json = JsonConvert.SerializeObject(lit, jsonSerializerSettings);
            
            Assert.AreEqual("\"hello\"", json);
        }

        [Test]
        public void DeserializingAJsonStringReturnsALiteralStringExpression()
        {
            const string json = "\"hello\"";
            
            var expr = JsonConvert.DeserializeObject<Expr<string>>(json, jsonSerializerSettings);

            Assert.IsInstanceOf<Expr>(expr);
            Assert.IsTrue(expr.IsValue);
            Assert.AreEqual("hello", (string)expr); // implicit → string
        }

        [Test]
        public void SerializingAnExpressionProducesArray()
        {
            // ["==", 5, 10]
            var expr = Expr.EqualsTo(5, 10);
            
            string json = JsonConvert.SerializeObject(expr, jsonSerializerSettings);
            
            Assert.AreEqual("[\"==\",5,10]", json);
        }

        [Test]
        public void DeserializingAJsonArrayReturnsAnExpression()
        {
            const string json = @"[""=="",5,10]";
            
            var expr = JsonConvert.DeserializeObject<Expr<bool>>(json, jsonSerializerSettings);

            Assert.IsTrue(expr.IsExpression);
            Assert.AreEqual(Operators.EqualTo, expr.Operator);

            // arguments should be two literals 5 and 10
            Assert.AreEqual(2, expr.Arguments.Length);

            Assert.IsInstanceOf<Expr<ExpressionValue>>(expr.Arguments[0]);
            Assert.IsInstanceOf<Expr<ExpressionValue>>(expr.Arguments[1]);

            var left = (Expr<ExpressionValue>)expr.Arguments[0];
            var right = (Expr<ExpressionValue>)expr.Arguments[1];
            
            Assert.IsTrue(left.Value.IsFloat);
            Assert.IsTrue(right.Value.IsFloat);
            Assert.AreEqual(5, (int)left);
            Assert.AreEqual(10, (int)right);
        }

        [Test]
        public void SerializingAnExpressionWithAVariableStatementReturnsAJsonArray()
        {
            // ["==", 5, ["get", "temperature"]]]]
            var expr = Expr.EqualsTo(5, Expr.GetVariable("temperature"));
            
            string json = JsonConvert.SerializeObject(expr, jsonSerializerSettings);
            
            Assert.AreEqual(@"[""=="",5,[""get"",""temperature""]]", json);
        }

        [Test]
        public void SerializingAComplexExpressionReturnsAJsonArray()
        {
            Expr<string> rgb = Expr.Rgb(
                Expr.GetVariable("temperature"), 
                0, 
                Expr.Min(100, Expr.GetVariable("temperature"))
            );
            
            string json = JsonConvert.SerializeObject(rgb, jsonSerializerSettings);

            const string expected = @"[""rgb"",[""get"",""temperature""],0,[""min"",100,[""get"",""temperature""]]]";
            
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void DeserializingAComplexExpressionReturnsCorrectComponents()
        {
            const string json = @"
            [
              ""rgb"",
              [""get"", ""temperature""],
              0,
              [""min"", 100, [""get"", ""temperature""]]
            ]";

            var expr = JsonConvert.DeserializeObject<Expr<string>>(json, jsonSerializerSettings);

            Assert.AreEqual(Operators.Rgb, expr.Operator);
            Assert.AreEqual(3, expr.Arguments.Length);

            // first arg: get temperature
            Expr<int> a0 = (Expr<int>)expr.Arguments[0];
            Assert.AreEqual(Operators.GetVariable, a0.Operator);
            Assert.AreEqual("temperature", (string)(Expr<string>)a0.Arguments[0]);

            // second arg: 0
            Expr<int> a1 = (Expr<int>)expr.Arguments[1];
            Assert.IsTrue(a1.IsValue);
            Assert.AreEqual(0, (int)a1);

            // third arg: min(100, get temperature)
            Expr<int> a2 = (Expr<int>)expr.Arguments[2];
            Assert.AreEqual(Operators.Min, a2.Operator);
            Assert.AreEqual(2, a2.Arguments.Length);

            Expr<int> m0 = (Expr<ExpressionValue>)a2.Arguments[0];
            Expr<ExpressionValue> m1 = (Expr<ExpressionValue>)a2.Arguments[1];
            Assert.AreEqual(100, (int)m0);
            Assert.AreEqual(Operators.GetVariable, m1.Operator);
            Assert.AreEqual("temperature", (string)(Expr<string>)m1.Arguments[0]);
        }
    }
}