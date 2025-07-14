using Newtonsoft.Json;
using NUnit.Framework;

namespace Netherlands3D.SerializableGisExpressions
{
    [TestFixture]
    public class ExpressionConverterTests
    {
        private JsonSerializerSettings jsonSerializerSettings;

        [SetUp]
        public void SetUp()
        {
            jsonSerializerSettings = new JsonSerializerSettings();
            jsonSerializerSettings.Converters.Add(new ExpressionConverter());
        }
        
        [Test]
        public void SerializingAnExpressionProducesArray()
        {
            // ["==", 5, 10]
            var expr = Expression.EqualTo(5, 10);
            
            string json = JsonConvert.SerializeObject(expr, jsonSerializerSettings);
            
            Assert.AreEqual("[\"==\",5,10]", json);
        }

        [Test]
        public void DeserializingAJsonArrayReturnsAnExpression()
        {
            const string json = @"[""=="",5,10]";
            
            var expr = JsonConvert.DeserializeObject<Expression>(json, jsonSerializerSettings);

            Assert.AreEqual(Expression.Operators.EqualTo, expr.Operator);

            // arguments should be two literals 5 and 10
            Assert.AreEqual(2, expr.Operands.Length);

            Assert.IsTrue(expr.Operand(0).IsNumber);
            Assert.IsTrue(expr.Operand(1).IsNumber);

            var left = expr.Operand(0);
            var right = expr.Operand(1);
            
            Assert.AreEqual(5, (int)left);
            Assert.AreEqual(10, (int)right);
        }

        [Test]
        public void SerializingAnExpressionWithAVariableStatementReturnsAJsonArray()
        {
            // ["==", 5, ["get", "temperature"]]]]
            var expr = Expression.EqualTo(5, Expression.Get("temperature"));
            
            string json = JsonConvert.SerializeObject(expr, jsonSerializerSettings);
            
            Assert.AreEqual(@"[""=="",5,[""get"",""temperature""]]", json);
        }

        [Test]
        public void SerializingAComplexExpressionReturnsAJsonArray()
        {
            Expression rgb = Expression.Rgb(
                Expression.Get("temperature"), 
                0, 
                Expression.Min(100, Expression.Get("temperature"))
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

            var expr = JsonConvert.DeserializeObject<Expression>(json, jsonSerializerSettings);

            Assert.AreEqual(Expression.Operators.Rgb, expr.Operator);
            Assert.AreEqual(3, expr.Operands.Length);

            // red/first arg: get temperature
            Expression red = expr.Operand(0);
            Assert.AreEqual(Expression.Operators.Get, red.Operator);
            Assert.AreEqual("temperature", (string)red.Operand(0));

            // green/second arg: primitive: 0
            var green = expr.Operand(1);
            Assert.IsTrue(green.IsInteger);
            Assert.AreEqual(0, (int)green);

            // blue/third arg: min(100, get temperature)
            Expression blue = expr.Operand(2);
            Assert.AreEqual(Expression.Operators.Min, blue?.Operator);
            Assert.AreEqual(2, blue.Operands.Length);

            var blueMinFirstOperand = blue.Operand(0);
            Expression blueMinSecondOperand = blue.Operand(1);
            Assert.AreEqual(100, (int)blueMinFirstOperand);
            Assert.AreEqual(Expression.Operators.Get, blueMinSecondOperand.Operator);
            Assert.AreEqual("temperature", (string)blueMinSecondOperand.Operand(0));
        }
    }
}