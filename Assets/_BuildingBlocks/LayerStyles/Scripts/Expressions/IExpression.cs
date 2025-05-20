namespace Netherlands3D.LayerStyles.Expressions
{
    public interface IExpression
    {
        public Operators Operator { get; }
        public IExpression[] Arguments { get; }
        
        public ExpressionValue Value { get; }
    }
}