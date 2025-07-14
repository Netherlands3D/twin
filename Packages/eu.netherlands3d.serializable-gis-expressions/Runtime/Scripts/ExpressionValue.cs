using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Netherlands3D.SerializableGisExpressions
{
    /// <summary>
    /// A discriminated wrapper around the possible results of evaluating an expression.
    /// Provides implicit conversions to/from common CLR types and type-query helpers.
    /// </summary>
    public readonly struct ExpressionValue
    {
        private readonly object value;

        /// <summary>
        /// Wraps any CLR value.
        /// </summary>
        public ExpressionValue(object value)
        {
            value = NormalizeArray(value);
            value = NormalizeDictionary(value);

            this.value = value;
        }

        /// <summary>
        /// Dictionaries are not covariant, and the type of the value can differ per dictionary. To support any
        /// type of Dictionary of string,object we need to convert it to an actual Dictionary of string,object.
        /// </summary>
        private static object NormalizeDictionary(object value)
        {
            if (value is not IDictionary nonGeneric) return value;
            
            var dict = new Dictionary<string, object>();
            foreach (DictionaryEntry entry in nonGeneric)
            {
                // copy the values so that we only allocate a new dictionary object, but not the values.
                if (entry.Key is string key) dict[key] = entry.Value;
            }

            return dict;
        }

        /// <summary>
        /// Only arrays with reference types are covariant with object[], so when we receive an array of value types
        /// we need to convert it to an array of objects for covariance.
        /// </summary>
        private static object NormalizeArray(object value)
        {
            if (value is not (Array arr and not object[])) return value;
            if (arr.GetType().GetElementType() is not { IsValueType: true }) return value;

            // Create a new object[] of the same length
            var output = new object[arr.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                // arr.GetValue(i) returns object (boxes value-types)
                output[i] = arr.GetValue(i);
            }

            value = output;

            return value;
        }

        public object Value => value;

        /// <summary>True if this value is a <see cref="bool"/>.</summary>
        public bool IsBoolean() => Value is bool;

        /// <summary>True if this value is an <see cref="int"/>.</summary>
        public bool IsInteger() => Value is int or uint or long or ulong or short or ushort or byte or sbyte;

        /// <summary>True if this value is a <see cref="float"/>.</summary>
        public bool IsFloat() => Value is float;

        /// <summary>True if this value is a <see cref="double"/>.</summary>
        public bool IsDouble() => Value is double;

        /// <summary>True if this value is any of the above numeric types.</summary>
        public bool IsNumber() => IsInteger() || IsFloat() || IsDouble();

        /// <summary>True if this value is a <see cref="string"/>.</summary>
        public bool IsString() => Value is string;

        /// <summary>True if this value is a <see cref="Color"/>.</summary>
        public bool IsColor() => Value is Color;

        /// <summary>True if this value is an <see cref="object"/> array.</summary>
        public bool IsArray() => Value is object[];

        /// <summary>True if this value is a json object, which is a Dictionary of string,object in C#</summary>
        public bool IsObject()
        {
            return value is IDictionary<string, object>;
        }

        /// <summary>Wraps a <see cref="bool"/> as an <see cref="ExpressionValue"/>.</summary>
        public static implicit operator ExpressionValue(bool b)
        {
            return new ExpressionValue(b);
        }

        /// <summary>Wraps an <see cref="int"/> as an <see cref="ExpressionValue"/>.</summary>
        public static implicit operator ExpressionValue(int i)
        {
            return new ExpressionValue(i);
        }

        /// <summary>Wraps a <see cref="float"/> as an <see cref="ExpressionValue"/>.</summary>
        public static implicit operator ExpressionValue(float f)
        {
            return new ExpressionValue(f);
        }

        /// <summary>Wraps a <see cref="double"/> as an <see cref="ExpressionValue"/>.</summary>
        public static implicit operator ExpressionValue(double d)
        {
            return new ExpressionValue(d);
        }

        /// <summary>Wraps a <see cref="string"/> as an <see cref="ExpressionValue"/>.</summary>
        public static implicit operator ExpressionValue(string s)
        {
            return new ExpressionValue(s);
        }

        /// <summary>Wraps a <see cref="Color"/> as an <see cref="ExpressionValue"/>.</summary>
        public static implicit operator ExpressionValue(Color c)
        {
            return new ExpressionValue(c);
        }

        /// <summary>Wraps an <see cref="object"/> array as an <see cref="ExpressionValue"/>.</summary>
        public static implicit operator ExpressionValue(object[] a)
        {
            return new ExpressionValue(a);
        }

        /// <summary>Wraps a JSON <see cref="object"/> object as an <see cref="ExpressionValue"/>.</summary>
        public static implicit operator ExpressionValue(Dictionary<string, object> a)
        {
            return new ExpressionValue(a);
        }

        //–– Implicit “down-cast” from ExpressionValue to CLR ––––––––––––––––––––––––––––––

        /// <summary>Unwraps a <see cref="bool"/>, or throws if not boolean.</summary>
        public static implicit operator bool(ExpressionValue ev)
        {
            if (ev.IsBoolean())
            {
                return (bool)ev.Value;
            }

            throw new InvalidCastException($"Cannot cast {ev.Value?.GetType().Name} to bool");
        }

        /// <summary>Unwraps an <see cref="int"/>, or converts if possible, else throws.</summary>
        public static implicit operator int(ExpressionValue ev)
        {
            if (ev.IsInteger())
            {
                return (int)ev.Value;
            }

            if (ev.Value is IConvertible convInt)
            {
                return convInt.ToInt32(CultureInfo.InvariantCulture);
            }

            throw new InvalidCastException($"Cannot cast {ev.Value?.GetType().Name} to int32");
        }

        /// <summary>Unwraps a <see cref="float"/>, or converts if possible, else throws.</summary>
        public static implicit operator float(ExpressionValue ev)
        {
            if (ev.IsFloat())
            {
                return (float)ev.Value;
            }

            if (ev.Value is IConvertible convFloat)
            {
                return convFloat.ToSingle(CultureInfo.InvariantCulture);
            }

            throw new InvalidCastException($"Cannot cast {ev.Value?.GetType().Name} to float");
        }

        /// <summary>Unwraps a <see cref="double"/>, or converts if possible, else throws.</summary>
        public static implicit operator double(ExpressionValue ev)
        {
            if (ev.IsDouble())
            {
                return (double)ev.Value;
            }

            if (ev.Value is IConvertible convDouble)
            {
                return convDouble.ToDouble(CultureInfo.InvariantCulture);
            }

            throw new InvalidCastException($"Cannot cast {ev.Value?.GetType().Name} to double");
        }

        /// <summary>Unwraps a <see cref="string"/>, returning null if underlying value is null.</summary>
        public static implicit operator string(ExpressionValue ev)
        {
            if (ev.Value == null)
            {
                return null;
            }

            return ev.Value.ToString();
        }

        /// <summary>Unwraps a <see cref="Color"/>, or throws if not a color.</summary>
        public static implicit operator Color(ExpressionValue ev)
        {
            if (ev.IsColor())
            {
                return (Color)ev.Value;
            }

            throw new InvalidCastException($"Cannot cast {ev.Value?.GetType().Name} to Color");
        }

        /// <summary>Unwraps an <see cref="object"/> array, or throws if not an array.</summary>
        public static implicit operator object[](ExpressionValue ev)
        {
            if (ev.IsArray())
            {
                return (object[])ev.Value;
            }

            throw new InvalidCastException($"Cannot cast {ev.Value?.GetType().Name} to object[]");
        }

        /// <summary>Unwraps an <see cref="object"/> array, or throws if not an array.</summary>
        public static implicit operator Dictionary<string, object>(ExpressionValue ev)
        {
            if (ev.IsObject())
            {
                return (Dictionary<string, object>)ev.Value;
            }

            throw new InvalidCastException($"Cannot cast {ev.Value?.GetType().Name} to an object (Dictionary<string, object>");
        }

        /// <summary>Returns a string representation of the underlying value (or "null").</summary>
        public override string ToString()
        {
            if (Value == null)
            {
                return "null";
            }

            return Value.ToString();
        }
    }
}