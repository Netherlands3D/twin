using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Netherlands3D.SerializableGisExpressions
{
    [TestFixture]
    public class ExpressionValueTest
    {
        [Test]
        public void ImplicitConversionToPrimitiveTypes()
        {
            ExpressionValue boolVal = true;
            ExpressionValue intVal = 42;
            ExpressionValue floatVal = 3.14f;
            ExpressionValue doubleVal = 2.718;
            ExpressionValue stringVal = "hello";

            Assert.AreEqual(true, (bool)boolVal);
            Assert.AreEqual(42, (int)intVal);
            Assert.AreEqual(3.14f, (float)floatVal);
            Assert.AreEqual(2.718, (double)doubleVal);
            Assert.AreEqual("hello", (string)stringVal);
        }

        [Test]
        public void ImplicitConversionToColor()
        {
            var color = new Color(1f, 0.5f, 0.25f, 0.75f);
            ExpressionValue colorVal = color;
            Color result = colorVal;
            Assert.AreEqual(color, result);
        }

        [Test]
        public void ImplicitConversionToObjectArray()
        {
            int[] intArr = { 1, 2, 3 };
            ExpressionValue evArr = new ExpressionValue(intArr);

            object[] result = (object[])evArr;
            Assert.IsInstanceOf<object[]>(result);
            CollectionAssert.AreEqual(new object[] { 1, 2, 3 }, result);

            // Equality check
            ExpressionValue evArr2 = new ExpressionValue(new object[] { 1, 2, 3 });
            Assert.IsTrue(evArr == evArr2);
            Assert.IsFalse(evArr != evArr2);
        }

        [Test]
        public void ImplicitConversionToDictionaryOfStringAndObject()
        {
            Hashtable table = new Hashtable { { "a", 1 }, { "b", 2 } };
            ExpressionValue value = new ExpressionValue(table);

            var result = (Dictionary<string, object>)value;
            Assert.AreEqual(1, result["a"]);
            Assert.AreEqual(2, result["b"]);

            var dict2 = new Dictionary<string, object> { { "a", 1 }, { "b", 2 } };
            ExpressionValue otherValue = new ExpressionValue(dict2);

            Assert.IsTrue(value.Equals(otherValue));
            Assert.AreEqual(value.GetHashCode(), otherValue.GetHashCode());
        }

        [Test]
        public void NullHandling()
        {
            ExpressionValue evNull1 = new ExpressionValue((object)null);
            ExpressionValue evNull2 = new ExpressionValue(null);

            Assert.IsTrue(evNull1 == evNull2);
            Assert.AreEqual(0, evNull1.GetHashCode());
            Assert.AreEqual("null", evNull1.ToString());
        }

        [Test]
        public void TypeStrictEquality()
        {
            ExpressionValue evIntZero = new ExpressionValue(0);
            ExpressionValue evStringZero = new ExpressionValue("0");
            
            Assert.IsFalse(evIntZero.Equals(evStringZero));
            Assert.IsFalse(evIntZero == evStringZero);
        }

        [Test]
        public void TypeStrictEqualityWithClrValue()
        {
            ExpressionValue evZero = new ExpressionValue(0);
            int evIntZero = 0;
            string evStringZero = "0";

            Assert.IsTrue(evZero.Equals(evIntZero));
            Assert.IsTrue(evZero == evIntZero);
            Assert.IsFalse(evZero.Equals(evStringZero));
            Assert.IsFalse(evZero == evStringZero);
        }

        [Test]
        public void InvalidCastsFromConvertibleThrowException()
        {
            ExpressionValue ev = new ExpressionValue("not a bool");

            // Convertible types will not understand the string so formatting fails
            Assert.Throws<FormatException>(() => { int i = ev; });
            Assert.Throws<FormatException>(() => { float f = ev; });
            Assert.Throws<FormatException>(() => { double d = ev; });

            // Non-convertible types will not understand the string so casting fails
            Assert.Throws<InvalidCastException>(() => { bool b = ev; });
            Assert.Throws<InvalidCastException>(() => { Color c = ev; });
            Assert.Throws<InvalidCastException>(() => { var arr = (object[])ev; });
            Assert.Throws<InvalidCastException>(() => { var dict = (Dictionary<string, object>)ev; });
        }

        [Test]
        public void InvalidCastsFromObjectThrowException()
        {
            ExpressionValue ev = new ExpressionValue(new Dictionary<string, object>());

            // Because the input is not convertible, casting to any type -except string- fails
            Assert.Throws<InvalidCastException>(() => { bool b = ev; });
            Assert.Throws<InvalidCastException>(() => { int i = ev; });
            Assert.Throws<InvalidCastException>(() => { float f = ev; });
            Assert.Throws<InvalidCastException>(() => { double d = ev; });
            Assert.Throws<InvalidCastException>(() => { Color c = ev; });
            Assert.Throws<InvalidCastException>(() => { var arr = (object[])ev; });

            Assert.AreEqual("System.Collections.Generic.Dictionary`2[System.String,System.Object]", (string)ev);
        }
    }
}