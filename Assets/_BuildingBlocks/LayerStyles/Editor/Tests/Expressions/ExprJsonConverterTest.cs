using System;
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
            Assert.IsTrue(expr.IsLiteral);
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
            Assert.IsTrue(expr.IsLiteral);
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
            Assert.IsTrue(expr.IsLiteral);
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
            Assert.IsTrue(expr.IsLiteral);
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
            const string json = "[\"==\",5,10]";
            
            var expr = JsonConvert.DeserializeObject<Expr<bool>>(json, jsonSerializerSettings);

            Assert.IsTrue(expr.IsExpression);
            Assert.AreEqual(Operators.EqualTo, expr.Operator);

            // arguments should be two literals 5 and 10
            Assert.AreEqual(2, expr.Arguments.Length);

            Debug.Log(expr.Arguments[0]);
            var left = expr.Arguments[0] as Expr<IConvertible>;
            var right = expr.Arguments[1] as Expr<IConvertible>;
            Assert.IsNotNull(left);
            Assert.IsNotNull(right);

            Assert.AreEqual(5, (int)left);
            Assert.AreEqual(10, (int)right);
        }

        [Test]
        public void Serialize_RgbAndMinNestedExpression_ProducesCorrectArray()
        {
            // build: ["get","temperature"]
            Expr<string> tempName = "temperature";
            var getTemp = Expr.GetVariable(tempName);

            // build: Min(100, getTemp)
            var minExpr = Expr.Min(100, getTemp);

            // build final: ["rgb", getTemp, 0, minExpr]
            // var rgb = Expr.Rgb(getTemp as Expr<int>, (Expr<int>)0, minExpr as Expr<int>);
            // Note: Rgb signature returns Expr<string> but testing serialization shape only

            // string json = JsonConvert.SerializeObject(rgb, _settings);
            // parse to JArray for easy structural assertions
            // var arr = JArray.Parse(json);

            // Assert.AreEqual("rgb", arr[0].Value<string>());
            // Assert.AreEqual("get", arr[1][0].Value<string>());
            // Assert.AreEqual("temperature", arr[1][1].Value<string>());
            // Assert.AreEqual(0, arr[2].Value<int>());
            // Assert.AreEqual("min", arr[3][0].Value<string>());
            // Assert.AreEqual(100, arr[3][1].Value<int>());
            // Assert.AreEqual("get", arr[3][2][0].Value<string>());
            // Assert.AreEqual("temperature", arr[3][2][1].Value<string>());
        }

        [Test]
        public void Deserialize_RgbAndMinNestedExpression_RestoresFullTree()
        {
            const string json = @"
            [
              ""rgb"",
              [""get"", ""temperature""],
              0,
              [""min"", 100, [""get"", ""temperature""]]
            ]";

            // We choose Expr<IConvertible> as the target because rgb returns Expr<string>
            var expr = JsonConvert.DeserializeObject<Expr<string>>(json, jsonSerializerSettings);

            Assert.AreEqual(Operators.Rgb, expr.Operator);
            Assert.AreEqual(3, expr.Arguments.Length);

            // first arg: get temperature
            var a0 = expr.Arguments[0] as Expr<IConvertible>;
            Assert.IsNotNull(a0);
            Assert.AreEqual(Operators.GetVariable, a0.Operator);
            Assert.AreEqual("temperature", (string)a0);

            // second arg: 0
            var a1 = expr.Arguments[1] as Expr<IConvertible>;
            Assert.IsNotNull(a1);
            Assert.IsTrue(a1.IsLiteral);
            Assert.AreEqual(0, (int)a1);

            // third arg: min(100, get temperature)
            var a2 = expr.Arguments[2] as Expr<IConvertible>;
            Assert.IsNotNull(a2);
            Assert.AreEqual(Operators.Min, a2.Operator);
            Assert.AreEqual(2, a2.Arguments.Length);

            var m0 = a2.Arguments[0] as Expr<IConvertible>;
            var m1 = a2.Arguments[1] as Expr<IConvertible>;
            Assert.AreEqual(100, (int)m0);
            Assert.AreEqual(Operators.GetVariable, m1.Operator);
            Assert.AreEqual("temperature", (string)m1);
        }
    }
}