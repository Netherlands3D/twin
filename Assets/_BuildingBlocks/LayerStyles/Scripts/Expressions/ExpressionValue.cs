using System;
using System.Collections.Generic;
using System.Linq;
using OneOf;
using UnityEngine;

namespace Netherlands3D.LayerStyles.Expressions
{
    // Must be List<ExpressionValue> and not ExpressionValue[] - the latter would make this recursive and Unity cannot
    // properly handle that in some situations, causing crashes
    public class ExpressionValue : OneOfBase<string, int, bool, float, double, List<ExpressionValue>>, IComparable, IConvertible
    {
        private ExpressionValue(OneOf<string, int, bool, float, double, List<ExpressionValue>> value) : base(value)
        {
        }

        public string AsString() => AsT0;
        public int AsInt() => AsT1;
        public bool AsBool() => AsT2;
        public float AsFloat() => AsT3;
        public double AsDouble() => AsT4;
        public List<ExpressionValue> AsCollection() => AsT5;
        
        public bool IsString => IsT0;
        public bool IsInt => IsT1;
        public bool IsBool => IsT2;
        public bool IsFloat => IsT3;
        public bool IsDouble => IsT4;
        public bool IsCollection => IsT5;

        public static implicit operator ExpressionValue(string v) => new(v);
        public static implicit operator ExpressionValue(int v) => new(v);
        public static implicit operator ExpressionValue(bool v) => new(v);
        public static implicit operator ExpressionValue(float v) => new(v);
        public static implicit operator ExpressionValue(double v) => new(v);
        public static implicit operator ExpressionValue(List<ExpressionValue> v) => new(v);
        public static implicit operator ExpressionValue(ExpressionValue[] v) => new(v.ToList());
        
        public static implicit operator ExpressionValue(string[] v) => new(v.Select(i => (ExpressionValue)i).ToList());
        public static implicit operator ExpressionValue(int[] v) => new(v.Select(i => (ExpressionValue)i).ToList());
        public static implicit operator ExpressionValue(bool[] v) => new(v.Select(i => (ExpressionValue)i).ToList());
        public static implicit operator ExpressionValue(float[] v) => new(v.Select(i => (ExpressionValue)i).ToList());
        public static implicit operator ExpressionValue(double[] v) => new(v.Select(i => (ExpressionValue)i).ToList());

        public static implicit operator double(ExpressionValue v) => v.ToDouble(null);
        public static implicit operator int(ExpressionValue v) => v.ToInt32(null);
        public static implicit operator float(ExpressionValue v) => v.ToSingle(null);
        public static implicit operator string(ExpressionValue v) => v.ToString(null);
        public static implicit operator bool(ExpressionValue v) => v.ToBoolean(null);
        public static implicit operator ExpressionValue[](ExpressionValue v) => v.AsCollection().ToArray();
        public static implicit operator List<ExpressionValue>(ExpressionValue v) => v.AsCollection();

        public int CompareTo(object other)
        {
            if (other is not ExpressionValue otherValue) return 0;
            
            if ((IsInt || IsFloat || IsDouble) && (otherValue.IsInt || otherValue.IsFloat || otherValue.IsDouble))
            {
                return ((IComparable)Value).CompareTo(otherValue.Value);
            }

            return 0;
        }

        public TypeCode GetTypeCode()
        {
            if (IsInt) return TypeCode.Int32;
            if (IsFloat) return TypeCode.Single;
            if (IsDouble) return TypeCode.Double;
            if (IsBool) return TypeCode.Boolean;
            if (IsString) return TypeCode.String;
            if (IsCollection) return TypeCode.Object;
            
            return TypeCode.Object;
        }

        public bool ToBoolean(IFormatProvider provider)
        {
            if (Value is IConvertible convertible) return convertible.ToBoolean(provider);

            throw new FormatException("Type is not compatible with Boolean");
        }

        public byte ToByte(IFormatProvider provider)
        {
            if (Value is IConvertible convertible) return convertible.ToByte(provider);

            throw new FormatException("Type is not compatible with Byte");
        }

        public char ToChar(IFormatProvider provider)
        {
            if (Value is IConvertible convertible) return convertible.ToChar(provider);

            throw new FormatException("Type is not compatible with Boolean");
        }

        public DateTime ToDateTime(IFormatProvider provider)
        {
            if (Value is IConvertible convertible) return convertible.ToDateTime(provider);

            throw new FormatException("Type is not compatible with DateTime");
        }

        public decimal ToDecimal(IFormatProvider provider)
        {
            if (Value is IConvertible convertible) return convertible.ToDecimal(provider);

            throw new FormatException("Type is not compatible with Decimal");
        }

        public double ToDouble(IFormatProvider provider)
        {
            if (Value is IConvertible convertible) return convertible.ToDouble(provider);

            throw new FormatException("Type is not compatible with Double");
        }

        public short ToInt16(IFormatProvider provider)
        {
            if (Value is IConvertible convertible) return convertible.ToInt16(provider);

            throw new FormatException("Type is not compatible with Int16");
        }

        public int ToInt32(IFormatProvider provider)
        {
            if (Value is IConvertible convertible) return convertible.ToInt32(provider);

            throw new FormatException("Type is not compatible with Int32");
        }

        public long ToInt64(IFormatProvider provider)
        {
            if (Value is IConvertible convertible) return convertible.ToChar(provider);

            throw new FormatException("Type is not compatible with Int64");
        }

        public sbyte ToSByte(IFormatProvider provider)
        {
            if (Value is IConvertible convertible) return convertible.ToSByte(provider);

            throw new FormatException("Type is not compatible with SByte");
        }

        public float ToSingle(IFormatProvider provider)
        {
            if (Value is IConvertible convertible) return convertible.ToSingle(provider);

            throw new FormatException("Type is not compatible with Float");
        }

        public string ToString(IFormatProvider provider)
        {
            if (Value == null) return null!;
            if (Value is IConvertible convertible) return convertible.ToString(provider);

            throw new FormatException("Type is not compatible with string");
        }

        public object ToType(Type conversionType, IFormatProvider provider)
        {
            if (Value is IConvertible convertible) return convertible.ToType(conversionType, provider);

            throw new FormatException("Type is not compatible with " + conversionType.FullName);
        }

        public ushort ToUInt16(IFormatProvider provider)
        {
            if (Value is IConvertible convertible) return convertible.ToUInt16(provider);

            throw new FormatException("Type is not compatible with UInt16");
        }

        public uint ToUInt32(IFormatProvider provider)
        {
            if (Value is IConvertible convertible) return convertible.ToUInt32(provider);

            throw new FormatException("Type is not compatible with UInt32");
        }

        public ulong ToUInt64(IFormatProvider provider)
        {
            if (Value is IConvertible convertible) return convertible.ToUInt64(provider);

            throw new FormatException("Type is not compatible with UInt64");
        }
    }
}