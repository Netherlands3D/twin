namespace Netherlands3D.LayerStyles
{
    public class StylingRule
    {
        public string Name { get; }
        public Symbolizer Symbolizer { get; } = new();

        // By default, everything matches. And that means a "True" literal boolean.
        public Expression Selector { get; } = BoolExpression.True();
    }
}