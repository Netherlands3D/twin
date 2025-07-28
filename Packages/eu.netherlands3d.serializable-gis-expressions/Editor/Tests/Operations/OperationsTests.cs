using System;
using NUnit.Framework;
using UnityEngine;

namespace Netherlands3D.SerializableGisExpressions.Operations
{
    [TestFixture]
    public class OperationsTests
    {
        #region IsNumber Tests
        [Test]
        public void IsNumberReturnsTrueForNumericTypes()
        {
            Assert.IsTrue(Operations.IsNumber((short)1));
            Assert.IsTrue(Operations.IsNumber((ushort)1));
            Assert.IsTrue(Operations.IsNumber(1));
            Assert.IsTrue(Operations.IsNumber(1u));
            Assert.IsTrue(Operations.IsNumber(1L));
            Assert.IsTrue(Operations.IsNumber(1UL));
            Assert.IsTrue(Operations.IsNumber(1.0f));
            Assert.IsTrue(Operations.IsNumber(1.0));
        }

        [Test]
        public void IsNumberReturnsFalseForNonNumericTypes()
        {
            Assert.IsFalse(Operations.IsNumber("123"));
            Assert.IsFalse(Operations.IsNumber(true));
            Assert.IsFalse(Operations.IsNumber(null));
            Assert.IsFalse(Operations.IsNumber(new object()));
        }
        #endregion

        #region ToDouble Tests
        [Test]
        public void ToDoubleConvertsPrimitiveNumbers()
        {
            Assert.AreEqual(42.0, Operations.ToDouble(42));
            Assert.AreEqual(3.14, Operations.ToDouble(3.14f), 1e-6);
            Assert.AreEqual(2.718, Operations.ToDouble(2.718));
        }

        [Test]
        public void ToDoubleUnwrapsExpressionValue()
        {
            var ev = new ExpressionValue(99);
            
            Assert.AreEqual(99.0, Operations.ToDouble(ev));
        }

        [Test]
        public void ToDoubleThrowsFormatExceptionForNonNumeric()
        {
            Assert.Throws<FormatException>(() => Operations.ToDouble("not a number"));
        }
        #endregion

        #region AsBool Tests
        [Test]
        public void AsBoolReturnsBoolForPrimitiveBool()
        {
            Assert.IsTrue(Operations.AsBool(true));
            Assert.IsFalse(Operations.AsBool(false));
        }

        [Test]
        public void AsBoolUnwrapsExpressionValue()
        {
            var evTrue = new ExpressionValue(true);
            var evFalse = new ExpressionValue(false);

            Assert.IsTrue(Operations.AsBool(evTrue));
            Assert.IsFalse(Operations.AsBool(evFalse));
        }

        [Test]
        public void AsBoolThrowsForNonBool()
        {
            Assert.Throws<InvalidOperationException>(() => Operations.AsBool(123));
            Assert.Throws<InvalidOperationException>(() => Operations.AsBool("true"));
            Assert.Throws<InvalidOperationException>(() => Operations.AsBool(null));
        }
        #endregion

        #region IsEqual Tests
        [Test]
        public void IsEqualComparesNumbersWithTolerance()
        {
            Assert.IsTrue(Operations.IsEqual(1.0000001, 1.0000002));
            Assert.IsFalse(Operations.IsEqual(1.0, 1.1));
        }

        [Test]
        public void IsEqualComparesStringsAndBools()
        {
            Assert.IsTrue(Operations.IsEqual("foo", "foo"));
            Assert.IsFalse(Operations.IsEqual("foo", "bar"));
            Assert.IsTrue(Operations.IsEqual(true, true));
            Assert.IsFalse(Operations.IsEqual(true, false));
        }

        [Test]
        public void IsEqualHandlesNullsAndReferences()
        {
            object a = null;
            object b = null;
            Assert.IsTrue(Operations.IsEqual(a, b));
            var obj = new object();
            Assert.IsTrue(Operations.IsEqual(obj, obj));
            Assert.IsFalse(Operations.IsEqual(null, obj));
        }

        [Test]
        public void IsEqualUnwrapsExpressionValue()
        {
            var ev1 = new ExpressionValue(5);
            var ev2 = new ExpressionValue(5.0);
            
            Assert.IsFalse(Operations.IsEqual(ev1, ev2), "IsEquals fails: types are not equal");
            Assert.IsTrue(Operations.IsEqual(ev1, 5), "IsEqual succeeds: int 5 and the expression value 5 is equal");
            Assert.IsTrue(Operations.IsEqual(5.0, ev2), "IsEqual succeeds: double 5 and the expression value 5.0 is equal");
            Assert.IsFalse(Operations.IsEqual(ev1, 6), "IsEqual fails: int 6 and the expression value 5 are not equal");
        }
        #endregion

        #region Compare Tests
        [Test]
        public void CompareNumericValues()
        {
            Assert.Less(Operations.Compare(1, 2), 0);
            Assert.AreEqual(0, Operations.Compare(2.5, 2.5));
            Assert.Greater(Operations.Compare(3.0, 2.9), 0);
        }

        [Test]
        public void CompareStrings()
        {
            Assert.Less(Operations.Compare("a", "b"), 0);
            Assert.AreEqual(0, Operations.Compare("foo", "foo"));
            Assert.Greater(Operations.Compare("z", "a"), 0);
        }

        [Test]
        public void CompareBooleans()
        {
            Assert.Less(Operations.Compare(false, true), 0);
            Assert.AreEqual(0, Operations.Compare(true, true));
            Assert.Greater(Operations.Compare(true, false), 0);
        }

        [Test]
        public void CompareNulls()
        {
            Assert.Less(Operations.Compare(null, 0), 0);
            Assert.Greater(Operations.Compare(0, null), 0);
            Assert.AreEqual(0, Operations.Compare(null, null));
        }

        [Test]
        public void CompareUnwrapsExpressionValue()
        {
            var ev1 = new ExpressionValue(1);
            var ev2 = new ExpressionValue(2);

            Assert.Less(Operations.Compare(ev1, ev2), 0);
            Assert.AreEqual(0, Operations.Compare(ev1, new ExpressionValue(1)));
        }

        [Test]
        public void CompareThrowsForIncomparable()
        {
            Assert.Throws<InvalidOperationException>(() => Operations.Compare(new object(), new object()));
        }
        #endregion

        #region GuardInRange Tests
        [Test]
        public void GuardInRangeDoesNotThrowForInRange()
        {
            Assert.DoesNotThrow(() => Operations.GuardInRange("code", "val", 5, 0, 10));
        }

        [Test]
        public void GuardInRangeThrowsForOutOfRange()
        {
            Assert.Throws<InvalidOperationException>(() => Operations.GuardInRange("code", "val", -1, 0, 10));
            Assert.Throws<InvalidOperationException>(() => Operations.GuardInRange("code", "val", 11, 0, 10));
        }
        #endregion

        #region Color Conversion Tests
        [Test]
        public void ConvertRgbToHsla()
        {
            // pure black => lightness 0
            var (h1, s1, l1) = Operations.ConvertRgbToHsla(Color.black);
            Assert.AreEqual(0, h1, 1e-6);
            Assert.AreEqual(0, s1, 1e-6);
            Assert.AreEqual(0, l1, 1e-6);

            // pure white => lightness 1
            var (h2, s2, l2) = Operations.ConvertRgbToHsla(Color.white);
            Assert.AreEqual(0, h2, 1e-6);
            Assert.AreEqual(0, s2, 1e-6);
            Assert.AreEqual(1, l2, 1e-6);

            // red
            var (h3, s3, l3) = Operations.ConvertRgbToHsla(Color.red);
            Assert.AreEqual(0, h3, 1e-6);
            Assert.AreEqual(1, s3, 1e-6);
            Assert.AreEqual(0.5, l3, 1e-6);
        }
        #endregion
    }
}