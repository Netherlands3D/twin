using System;

namespace Netherlands3D.LayerStyles.Expressions
{
    public interface IExpression
    {
        public string Operator { get; }
        public IExpression[] Arguments { get; }
        
        public IConvertible Value { get; }
    }
}