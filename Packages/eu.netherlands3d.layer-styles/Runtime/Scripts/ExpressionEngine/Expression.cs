using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Op = Netherlands3D.LayerStyles.ExpressionEngine.OperatorHandlers;

namespace Netherlands3D.LayerStyles.ExpressionEngine
{
    public partial class Expression
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum Operators
        {
            [EnumMember(Value = Op.ArrayOperator.Code)]
            Array,

            [EnumMember(Value = Op.BooleanOperator.Code)]
            Boolean,
            [EnumMember(Value = "collator")] Collator,
            [EnumMember(Value = "format")] Format,
            [EnumMember(Value = "image")] Image,

            [EnumMember(Value = Op.LiteralOperator.Code)]
            Literal,

            [EnumMember(Value = Op.NumberOperator.Code)]
            Number,

            [EnumMember(Value = Op.NumberFormatOperator.Code)]
            NumberFormat,

            [EnumMember(Value = Op.ObjectOperator.Code)]
            Object,

            [EnumMember(Value = Op.StringOperator.Code)]
            String,

            [EnumMember(Value = Op.ToBooleanOperator.Code)]
            ToBoolean,

            [EnumMember(Value = Op.ToColorOperator.Code)]
            ToColor,

            [EnumMember(Value = Op.ToNumberOperator.Code)]
            ToNumber,

            [EnumMember(Value = Op.ToStringOperator.Code)]
            ToString,

            [EnumMember(Value = Op.TypeOfOperator.Code)]
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

            [EnumMember(Value = Op.GetOperator.Code)]
            Get,
            [EnumMember(Value = "has")] Has,
            [EnumMember(Value = "in")] In,
            [EnumMember(Value = "index-of")] IndexOf,
            [EnumMember(Value = "length")] Length,
            [EnumMember(Value = "measure-light")] MeasureLight,
            [EnumMember(Value = "slice")] Slice,

            [EnumMember(Value = Op.NotOperator.Code)]
            Not,

            [EnumMember(Value = Op.NotEqualOperator.Code)]
            NotEqual,

            [EnumMember(Value = Op.LessThanOperator.Code)]
            LessThan,

            [EnumMember(Value = Op.LessThanOrEqualOperator.Code)]
            LessThanOrEqual,

            [EnumMember(Value = Op.EqualOperator.Code)]
            Equal,

            [EnumMember(Value = Op.GreaterThanOperator.Code)]
            GreaterThan,

            [EnumMember(Value = Op.GreaterThanOrEqualOperator.Code)]
            GreaterThanOrEqual,

            [EnumMember(Value = Op.AllOperator.Code)]
            All,

            [EnumMember(Value = Op.AnyOperator.Code)]
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

            [EnumMember(Value = Op.HslOperator.Code)] Hsl,
            [EnumMember(Value = Op.HslaOperator.Code)] Hsla,
            [EnumMember(Value = Op.RgbOperator.Code)] Rgb,
            [EnumMember(Value = Op.RgbaOperator.Code)] Rgba,
            [EnumMember(Value = Op.ToHslaOperator.Code)] ToHsla,
            [EnumMember(Value = Op.ToRgbaOperator.Code)] ToRgba,

            [EnumMember(Value = Op.SubtractOperator.Code)]
            Subtract,

            [EnumMember(Value = Op.MultiplyOperator.Code)]
            Multiply,

            [EnumMember(Value = Op.DivideOperator.Code)]
            Divide,

            [EnumMember(Value = Op.ModuloOperator.Code)]
            Modulo,

            [EnumMember(Value = Op.PowerOperator.Code)]
            Power,

            [EnumMember(Value = Op.AddOperator.Code)]
            Add,

            [EnumMember(Value = Op.AbsOperator.Code)]
            Abs,

            [EnumMember(Value = Op.AcosOperator.Code)]
            Acos,

            [EnumMember(Value = Op.AsinOperator.Code)]
            Asin,

            [EnumMember(Value = Op.AtanOperator.Code)]
            Atan,

            [EnumMember(Value = Op.CeilOperator.Code)]
            Ceil,

            [EnumMember(Value = Op.CosOperator.Code)]
            Cos,

            [EnumMember(Value = Op.DistanceOperator.Code)]
            Distance,

            [EnumMember(Value = Op.EOperator.Code)]
            E,

            [EnumMember(Value = Op.FloorOperator.Code)]
            Floor,

            [EnumMember(Value = Op.LnOperator.Code)]
            Ln,

            [EnumMember(Value = Op.Ln2Operator.Code)]
            Ln2,

            [EnumMember(Value = Op.Log10Operator.Code)]
            Log10,

            [EnumMember(Value = Op.Log2Operator.Code)]
            Log2,

            [EnumMember(Value = Op.MaxOperator.Code)]
            Max,

            [EnumMember(Value = Op.MinOperator.Code)]
            Min,

            [EnumMember(Value = Op.PiOperator.Code)]
            Pi,

            [EnumMember(Value = Op.RandomOperator.Code)]
            Random,

            [EnumMember(Value = Op.RoundOperator.Code)]
            Round,

            [EnumMember(Value = Op.SinOperator.Code)]
            Sin,

            [EnumMember(Value = Op.SqrtOperator.Code)]
            Sqrt,

            [EnumMember(Value = Op.TanOperator.Code)]
            Tan,

            [EnumMember(Value = "distance-from-center")]
            DistanceFromCenter,
            [EnumMember(Value = "pitch")] Pitch,
            [EnumMember(Value = "zoom")] Zoom,

            [EnumMember(Value = "heatmap-density")]
            HeatmapDensity,
        }

        public Operators Operator;
        public object[] Operands;

        public Expression(Operators @operator, params object[] operands)
        {
            Operator = @operator;
            Operands = operands;
        }
    }
}