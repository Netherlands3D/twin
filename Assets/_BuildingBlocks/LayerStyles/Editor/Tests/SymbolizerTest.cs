using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Netherlands3D.LayerStyles
{
    public class SymbolizerTests
    {
        [Test]
        public void SetAndGetFillColorReturnsCorrectColor()
        {
            var symbolizer = new Symbolizer();
            
            var expected = new Color(0.1f, 0.2f, 0.3f, 0.4f);

            symbolizer.SetFillColor(expected);
            var actual = symbolizer.GetFillColor();

            Assert.That(actual.HasValue, Is.True);
            Assert.That(actual.Value, Is.EqualTo(expected).Using(ColorEqualityComparer.Instance));
        }

        [Test]
        public void SetAndGetStrokeColorReturnsCorrectColor()
        {
            var symbolizer = new Symbolizer();
            
            var expected = new Color(0.1f, 0.2f, 0.3f, 0.4f);

            symbolizer.SetStrokeColor(expected);
            var actual = symbolizer.GetStrokeColor();

            Assert.That(actual.HasValue, Is.True);
            Assert.That(actual.Value, Is.EqualTo(expected).Using(ColorEqualityComparer.Instance));
        }

        [Test]
        public void GettingColorsReturnsNullByDefault()
        {
            var symbolizer = new Symbolizer();

            Assert.That(symbolizer.GetFillColor(), Is.Null);
            Assert.That(symbolizer.GetStrokeColor(), Is.Null);
        }

        [Test]
        public void GetColorCorrectsMissingHashSign()
        {
            var symbolizer = new Symbolizer();

            var color = new Color(1f, 0.5f, 0f, 1f);
            var hex = ColorUtility.ToHtmlStringRGBA(color).ToLower(); // without '#'

            // Inject old format directly into private field using reflection
            var propField = typeof(Symbolizer).GetField("properties", BindingFlags.NonPublic | BindingFlags.Instance);
            var props = new Dictionary<string, string> { { "fill-color", hex } };
            propField.SetValue(symbolizer, props);

            var result = symbolizer.GetFillColor();
            Assert.That(result.HasValue, Is.True);
            Assert.That(result.Value, Is.EqualTo(color).Using(ColorEqualityComparer.Instance));
        }

        [Test]
        public void MergingCombinesPropertiesFromAnotherSymbolizer()
        {
            var red = Color.red;
            var blue = Color.blue;

            // base has a red fill and blue stroke
            var baseSymbolizer = new Symbolizer();
            baseSymbolizer.SetFillColor(red);
            baseSymbolizer.SetStrokeColor(blue);
            
            // other one only has a blue fill
            var otherSymbolizer = new Symbolizer();
            otherSymbolizer.SetFillColor(blue);

            var result = Symbolizer.Merge(baseSymbolizer, otherSymbolizer);

            // end result should have both a blue fill and stroke
            Assert.That(result.GetFillColor(), Is.EqualTo(blue).Using(ColorEqualityComparer.Instance));
            Assert.That(result.GetStrokeColor(), Is.EqualTo(blue).Using(ColorEqualityComparer.Instance));
        }

        /// <summary>
        /// Custom comparer to handle conversion inaccuracies in color components from hex values to floats.
        /// </summary>
        private class ColorEqualityComparer : IEqualityComparer<Color>
        {
            public static readonly ColorEqualityComparer Instance = new();

            public bool Equals(Color x, Color y)
            {
                return Mathf.Abs(y.r - x.r) < 0.005f 
                       && Mathf.Abs(y.g - x.g) < 0.005f 
                       && Mathf.Abs(y.b - x.b) < 0.005f
                       && Mathf.Abs(y.a - x.a) < 0.005f;
            }

            public int GetHashCode(Color obj) => obj.GetHashCode();
        }
    }
}
