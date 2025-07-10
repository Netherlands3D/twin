using System;
using System.Globalization;
using System.Runtime.Serialization;
using Netherlands3D.LayerStyles.Expressions.OperatorHandlers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace Netherlands3D.LayerStyles.Expressions
{
    public partial class Expression
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum Operators
        {
            [EnumMember(Value = ArrayOperator.Code)]
            Array,

            [EnumMember(Value = BooleanOperator.Code)]
            Boolean,
            [EnumMember(Value = "collator")] Collator,
            [EnumMember(Value = "format")] Format,
            [EnumMember(Value = "image")] Image,

            [EnumMember(Value = LiteralOperator.Code)]
            Literal,

            [EnumMember(Value = NumberOperator.Code)]
            Number,

            [EnumMember(Value = NumberFormatOperator.Code)]
            NumberFormat,

            [EnumMember(Value = ObjectOperator.Code)]
            Object,

            [EnumMember(Value = StringOperator.Code)]
            String,

            [EnumMember(Value = ToBooleanOperator.Code)]
            ToBoolean,

            [EnumMember(Value = ToColorOperator.Code)]
            ToColor,

            [EnumMember(Value = ToNumberOperator.Code)]
            ToNumber,

            [EnumMember(Value = ToStringOperator.Code)]
            ToString,

            [EnumMember(Value = TypeOfOperator.Code)]
            TypeOf,

            [EnumMember(Value = "accumulated")] Accumulated,
            [EnumMember(Value = "feature-state")] FeatureState,
            [EnumMember(Value = "geometry-type")] GeometryType,
            [EnumMember(Value = "id")] Id,
            [EnumMember(Value = "line-progress")] LineProgress,
            [EnumMember(Value = "properties")] Properties,

            [EnumMember(Value = "at")] At,

            [EnumMember(Value = "at-interpolated")]
            AtInterpolated,
            [EnumMember(Value = "config")] Config,

            [EnumMember(Value = GetOperator.Code)]
            Get,
            [EnumMember(Value = "has")] Has,
            [EnumMember(Value = "in")] In,
            [EnumMember(Value = "index-of")] IndexOf,
            [EnumMember(Value = "length")] Length,
            [EnumMember(Value = "measure-light")] MeasureLight,
            [EnumMember(Value = "slice")] Slice,

            [EnumMember(Value = NotOperator.Code)]
            Not,

            [EnumMember(Value = NotEqualOperator.Code)]
            NotEqual,

            [EnumMember(Value = LessThanOperator.Code)]
            LessThan,

            [EnumMember(Value = LessThanOrEqualOperator.Code)]
            LessThanOrEqual,

            [EnumMember(Value = EqualOperator.Code)]
            EqualTo,

            [EnumMember(Value = GreaterThanOperator.Code)]
            GreaterThan,

            [EnumMember(Value = GreaterThanOrEqualOperator.Code)]
            GreaterThanOrEqual,

            [EnumMember(Value = AllOperator.Code)]
            All,

            [EnumMember(Value = AnyOperator.Code)]
            Any,
            [EnumMember(Value = "case")] Case,
            [EnumMember(Value = "coalesce")] Coalesce,
            [EnumMember(Value = "match")] Match,
            [EnumMember(Value = "within")] Within,

            [EnumMember(Value = "interpolate")] Interpolate,

            [EnumMember(Value = "interpolate-hcl")]
            InterpolateHcl,

            [EnumMember(Value = "interpolate-lab")]
            InterpolateLab,
            [EnumMember(Value = "step")] Step,
            [EnumMember(Value = "let")] Let,
            [EnumMember(Value = "var")] Var,
            [EnumMember(Value = "concat")] Concat,
            [EnumMember(Value = "downcase")] Downcase,

            [EnumMember(Value = "is-supported-script")]
            IsSupportedScript,

            [EnumMember(Value = "resolved-locale")]
            ResolvedLocale,
            [EnumMember(Value = "upcase")] Upcase,

            [EnumMember(Value = HslOperator.Code)]
            Hsl,

            [EnumMember(Value = HslaOperator.Code)]
            Hsla,

            [EnumMember(Value = RgbOperator.Code)]
            Rgb,

            [EnumMember(Value = RgbaOperator.Code)]
            Rgba,

            [EnumMember(Value = ToHslaOperator.Code)]
            ToHsla,

            [EnumMember(Value = ToRgbaOperator.Code)]
            ToRgba,

            [EnumMember(Value = SubtractOperator.Code)]
            Subtract,

            [EnumMember(Value = MultiplyOperator.Code)]
            Multiply,

            [EnumMember(Value = DivideOperator.Code)]
            Divide,

            [EnumMember(Value = ModuloOperator.Code)]
            Modulo,

            [EnumMember(Value = PowerOperator.Code)]
            Power,

            [EnumMember(Value = AddOperator.Code)]
            Add,

            [EnumMember(Value = AbsOperator.Code)]
            Abs,

            [EnumMember(Value = AcosOperator.Code)]
            Acos,

            [EnumMember(Value = AsinOperator.Code)]
            Asin,

            [EnumMember(Value = AtanOperator.Code)]
            Atan,

            [EnumMember(Value = CeilOperator.Code)]
            Ceil,

            [EnumMember(Value = CosOperator.Code)]
            Cos,

            [EnumMember(Value = DistanceOperator.Code)]
            Distance,

            [EnumMember(Value = EOperator.Code)]
            E,

            [EnumMember(Value = FloorOperator.Code)]
            Floor,

            [EnumMember(Value = LnOperator.Code)]
            Ln,

            [EnumMember(Value = Ln2Operator.Code)]
            Ln2,

            [EnumMember(Value = Log10Operator.Code)]
            Log10,

            [EnumMember(Value = Log2Operator.Code)]
            Log2,

            [EnumMember(Value = MaxOperator.Code)]
            Max,

            [EnumMember(Value = MinOperator.Code)]
            Min,

            [EnumMember(Value = PiOperator.Code)]
            Pi,

            [EnumMember(Value = RandomOperator.Code)]
            Random,

            [EnumMember(Value = RoundOperator.Code)]
            Round,

            [EnumMember(Value = SinOperator.Code)]
            Sin,

            [EnumMember(Value = SqrtOperator.Code)]
            Sqrt,

            [EnumMember(Value = TanOperator.Code)]
            Tan,

            [EnumMember(Value = "distance-from-center")]
            DistanceFromCenter,
            [EnumMember(Value = "pitch")] Pitch,
            [EnumMember(Value = "zoom")] Zoom,

            [EnumMember(Value = "heatmap-density")]
            HeatmapDensity,
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
            public bool IsNumber => ExpressionEvaluator.IsNumber(value);
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

        public Operators Operator;
        public object[] Operands;
        public OperandView Operand(int index) => new OperandView(Operands[index]);

        public Expression(Operators @operator, params object[] operands)
        {
            Operator = @operator;
            Operands = operands;
        }
    }
}