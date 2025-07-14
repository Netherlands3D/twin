using System;
using System.Globalization;
using System.Runtime.Serialization;
using Netherlands3D.LayerStyles.Expressions.Operations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace Netherlands3D.LayerStyles.Expressions
{
    public partial class Expression
    {
        /// <summary>
        /// List of all supported expression operators.
        ///
        /// Any commented operators are not yet supported but mentioned in the MApbox expression spec
        /// (https://docs.mapbox.com/style-spec/reference/expressions/) and can be added later.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum Operators
        {
            [EnumMember(Value = ArrayOperation.Code)]
            Array,

            [EnumMember(Value = BooleanOperation.Code)]
            Boolean,
            
            // [EnumMember(Value = "collator")] 
            // Collator,
            
            // [EnumMember(Value = "format")] 
            // Format,
            
            // [EnumMember(Value = "image")] 
            // Image,

            [EnumMember(Value = LiteralOperation.Code)]
            Literal,

            [EnumMember(Value = NumberOperation.Code)]
            Number,

            [EnumMember(Value = NumberFormatOperation.Code)]
            NumberFormat,

            [EnumMember(Value = ObjectOperation.Code)]
            Object,

            [EnumMember(Value = StringOperation.Code)]
            String,

            [EnumMember(Value = ToBooleanOperation.Code)]
            ToBoolean,

            [EnumMember(Value = ToColorOperation.Code)]
            ToColor,

            [EnumMember(Value = ToNumberOperation.Code)]
            ToNumber,

            [EnumMember(Value = ToStringOperation.Code)]
            ToString,

            [EnumMember(Value = TypeOfOperation.Code)]
            TypeOf,

            // [EnumMember(Value = "accumulated")] 
            // Accumulated,
            
            // [EnumMember(Value = "feature-state")] 
            // FeatureState,
            
            // [EnumMember(Value = "geometry-type")] 
            // GeometryType,
           
            // [EnumMember(Value = "id")] 
            // Id,
            
            // [EnumMember(Value = "line-progress")] 
            // LineProgress,
            
            // [EnumMember(Value = "properties")] 
            // Properties,

            // [EnumMember(Value = "at")] 
            // At,

            // [EnumMember(Value = "at-interpolated")]
            // AtInterpolated,

            // [EnumMember(Value = "config")] 
            // Config,

            [EnumMember(Value = GetOperation.Code)]
            Get,
            
            // [EnumMember(Value = "has")] 
            // Has,
            
            // [EnumMember(Value = "in")] 
            // In,
            
            // [EnumMember(Value = "index-of")] 
            // IndexOf,
            
            // [EnumMember(Value = "length")] 
            // Length,
            
            // [EnumMember(Value = "measure-light")] 
            // MeasureLight,
            
            // [EnumMember(Value = "slice")] 
            // Slice,

            [EnumMember(Value = NotOperation.Code)]
            Not,

            [EnumMember(Value = NotEqualOperation.Code)]
            NotEqual,

            [EnumMember(Value = LessThanOperation.Code)]
            LessThan,

            [EnumMember(Value = LessThanOrEqualOperation.Code)]
            LessThanOrEqual,

            [EnumMember(Value = EqualOperation.Code)]
            EqualTo,

            [EnumMember(Value = GreaterThanOperation.Code)]
            GreaterThan,

            [EnumMember(Value = GreaterThanOrEqualOperation.Code)]
            GreaterThanOrEqual,

            [EnumMember(Value = AllOperation.Code)]
            All,

            [EnumMember(Value = AnyOperation.Code)]
            Any,
            
            // [EnumMember(Value = "case")] 
            // Case,
            
            // [EnumMember(Value = "coalesce")] 
            // Coalesce,
            
            // [EnumMember(Value = "match")] 
            // Match,
            
            // [EnumMember(Value = "within")] 
            // Within,

            // [EnumMember(Value = "interpolate")] 
            // Interpolate,

            // [EnumMember(Value = "interpolate-hcl")]
            // InterpolateHcl,

            // [EnumMember(Value = "interpolate-lab")]
            // InterpolateLab,
            
            // [EnumMember(Value = "step")] 
            // Step,
            
            // [EnumMember(Value = "let")] 
            // Let,
            
            // [EnumMember(Value = "var")] 
            // Var,
            
            // [EnumMember(Value = "concat")] 
            // Concat,
            
            // [EnumMember(Value = "downcase")] 
            // Downcase,

            // [EnumMember(Value = "is-supported-script")]
            // IsSupportedScript,

            // [EnumMember(Value = "resolved-locale")]
            // ResolvedLocale,
            
            // [EnumMember(Value = "upcase")] 
            // Upcase,

            [EnumMember(Value = HslOperation.Code)]
            Hsl,

            [EnumMember(Value = HslaOperation.Code)]
            Hsla,

            [EnumMember(Value = RgbOperation.Code)]
            Rgb,

            [EnumMember(Value = RgbaOperation.Code)]
            Rgba,

            [EnumMember(Value = ToHslaOperation.Code)]
            ToHsla,

            [EnumMember(Value = ToRgbaOperation.Code)]
            ToRgba,

            [EnumMember(Value = SubtractOperation.Code)]
            Subtract,

            [EnumMember(Value = MultiplyOperation.Code)]
            Multiply,

            [EnumMember(Value = DivideOperation.Code)]
            Divide,

            [EnumMember(Value = ModuloOperation.Code)]
            Modulo,

            [EnumMember(Value = PowerOperation.Code)]
            Power,

            [EnumMember(Value = AddOperation.Code)]
            Add,

            [EnumMember(Value = AbsOperation.Code)]
            Abs,

            [EnumMember(Value = AcosOperation.Code)]
            Acos,

            [EnumMember(Value = AsinOperation.Code)]
            Asin,

            [EnumMember(Value = AtanOperation.Code)]
            Atan,

            [EnumMember(Value = CeilOperation.Code)]
            Ceil,

            [EnumMember(Value = CosOperation.Code)]
            Cos,

            // [EnumMember(Value = "distance")]
            // Distance,

            [EnumMember(Value = EOperation.Code)]
            E,

            [EnumMember(Value = FloorOperation.Code)]
            Floor,

            [EnumMember(Value = LnOperation.Code)]
            Ln,

            [EnumMember(Value = Ln2Operation.Code)]
            Ln2,

            [EnumMember(Value = Log10Operation.Code)]
            Log10,

            [EnumMember(Value = Log2Operation.Code)]
            Log2,

            [EnumMember(Value = MaxOperation.Code)]
            Max,

            [EnumMember(Value = MinOperation.Code)]
            Min,

            [EnumMember(Value = PiOperation.Code)]
            Pi,

            [EnumMember(Value = RandomOperation.Code)]
            Random,

            [EnumMember(Value = RoundOperation.Code)]
            Round,

            [EnumMember(Value = SinOperation.Code)]
            Sin,

            [EnumMember(Value = SqrtOperation.Code)]
            Sqrt,

            [EnumMember(Value = TanOperation.Code)]
            Tan,

            // [EnumMember(Value = "distance-from-center")]
            // DistanceFromCenter,
            
            // [EnumMember(Value = "pitch")] 
            // Pitch,
            
            // [EnumMember(Value = "zoom")] 
            // Zoom,

            // [EnumMember(Value = "heatmap-density")]
            // HeatmapDensity,
        }

        /// <summary>
        /// A thin value‐type wrapper around one of an Expression’s operands,
        /// with easy kind‐checking and conversions.
        /// </summary>
        public readonly struct OperandView
        {
            private readonly object value;
            public OperandView(object value) => this.value = value;

            // We do not distinguish between int or long in this function as we assume the implicit operator
            // or the AsX functions will capture that; for determining whether this is an integer we declare both
            // similar (as in: discrete numbers)
            public bool IsInteger => value is short or ushort or int or uint or long or ulong;
            public bool IsFloat => value is float;
            public bool IsDouble => value is double;
            public bool IsNumber => Operations.Operations.IsNumber(value);
            public bool IsString => value is string;
            public bool IsBoolean => value is bool;
            public bool IsColor => value is Color;
            public bool IsArray => value is object[];
            public bool IsExpression => value is Expression;
            public bool IsNull => value is null;

            public short AsShort => Convert.ToInt16(value, CultureInfo.InvariantCulture);
            public int AsInteger => Convert.ToInt32(value, CultureInfo.InvariantCulture);
            public long AsLong => Convert.ToInt64(value, CultureInfo.InvariantCulture);
            public double AsDouble => Convert.ToDouble(value, CultureInfo.InvariantCulture);
            public string AsString => value?.ToString();
            public bool AsBoolean => (bool)value;
            public Color AsColor => (Color)value;
            public object[] AsArray => (object[])value;
            public Expression AsExpression => (Expression)value;

            public static implicit operator short(OperandView o) => o.AsShort;
            public static implicit operator int(OperandView o) => o.AsInteger;
            public static implicit operator long(OperandView o) => o.AsLong;
            public static implicit operator double(OperandView o) => o.AsDouble;
            public static implicit operator string(OperandView o) => o.AsString;
            public static implicit operator bool(OperandView o) => o.AsBoolean;
            public static implicit operator Color(OperandView o) => o.AsColor;
            public static implicit operator object[](OperandView o) => o.AsArray;
            public static implicit operator Expression(OperandView o) => o.AsExpression;
        }

        public readonly Operators Operator;
        public readonly object[] Operands;
        public OperandView Operand(int index) => new OperandView(Operands[index]);

        public Expression(Operators @operator, params object[] operands)
        {
            Operator = @operator;
            Operands = operands;
        }
    }
}