using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Netherlands3D.LayerStyles.Expressions
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Operators
    {
        [EnumMember(Value = "literal")] Literal,

        [EnumMember(Value = "==")] EqualTo,

        [EnumMember(Value = ">")] GreaterThan,

        [EnumMember(Value = ">=")] GreaterThanOrEqual,

        [EnumMember(Value = "<")] LessThan,

        [EnumMember(Value = "<=")] LessThanOrEqual,

        [EnumMember(Value = "min")] Min,

        [EnumMember(Value = "rgb")] Rgb,

        [EnumMember(Value = "get")] GetVariable,

        [EnumMember(Value = "in")] In
    }
}