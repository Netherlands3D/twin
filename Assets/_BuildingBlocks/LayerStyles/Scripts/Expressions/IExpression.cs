using Newtonsoft.Json;

namespace Netherlands3D.LayerStyles.Expressions
{
    [JsonConverter(typeof(ExprJsonConverter))]
    public interface IExpression
    {
        public Operators Operator { get; }
        public IExpression[] Arguments { get; }
        
        public ExpressionValue Value { get; }
        
        public bool IsValue { get; }
        public bool IsExpression { get; }
    }
}