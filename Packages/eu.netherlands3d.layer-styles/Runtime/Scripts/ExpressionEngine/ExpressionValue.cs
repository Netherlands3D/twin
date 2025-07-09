using System;
using System.Globalization;
using UnityEngine;

namespace Netherlands3D.LayerStyles.ExpressionEngine
{
    /// <summary>
    /// A discriminated wrapper around the possible expression result types,
    /// with implicit conversions to and from the CLR types for convenience.
    /// </summary>
    public readonly struct ExpressionValue
    {
        private readonly object value;

        public ExpressionValue(object value)
        {
            this.value = value;
        }

        // Implicit “up‐cast” from CLR → ExpressionValue
        public static implicit operator ExpressionValue(bool b) => new(b);
        public static implicit operator ExpressionValue(int d) => new(d);
        public static implicit operator ExpressionValue(float d) => new(d);
        public static implicit operator ExpressionValue(double d) => new(d);
        public static implicit operator ExpressionValue(string s) => new(s);
        public static implicit operator ExpressionValue(Color c) => new(c);
        public static implicit operator ExpressionValue(object[] a) => new(a);

        // Implicit “down‐cast” from ExpressionValue → CLR
        public static implicit operator bool(ExpressionValue ev)
            => ev.value is bool b
                ? b
                : throw new InvalidCastException($"Cannot cast {ev.value?.GetType().Name} to bool");

        public static implicit operator int(ExpressionValue ev)
            => ev.value is int i ? i
                : ev.value is IConvertible conv ? conv.ToInt32(CultureInfo.InvariantCulture)
                : throw new InvalidCastException($"Cannot cast {ev.value?.GetType().Name} to int32");

        public static implicit operator float(ExpressionValue ev)
            => ev.value is float f ? f
                : ev.value is IConvertible conv ? conv.ToSingle(CultureInfo.InvariantCulture)
                : throw new InvalidCastException($"Cannot cast {ev.value?.GetType().Name} to float");

        public static implicit operator double(ExpressionValue ev)
            => ev.value is double d ? d
                : ev.value is IConvertible conv ? conv.ToDouble(CultureInfo.InvariantCulture)
                : throw new InvalidCastException($"Cannot cast {ev.value?.GetType().Name} to double");

        public static implicit operator string(ExpressionValue ev)
            => ev.value?.ToString();

        public static implicit operator Color(ExpressionValue ev)
            => ev.value is Color c
                ? c
                : throw new InvalidCastException($"Cannot cast {ev.value?.GetType().Name} to Color");

        public static implicit operator object[](ExpressionValue ev)
            => ev.value as object[]
               ?? throw new InvalidCastException($"Cannot cast {ev.value?.GetType().Name} to object[]");

        public override string ToString() => value?.ToString() ?? "null";
    }
}