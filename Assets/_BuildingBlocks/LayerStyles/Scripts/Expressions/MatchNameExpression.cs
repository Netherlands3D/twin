using System.Runtime.Serialization;

namespace Netherlands3D.LayerStyles.Expressions
{
    /// <summary>
    /// A literal piece of text.
    ///
    /// This is used in places - such as the ExpressionContext - where you need a literal piece of text to resolve
    /// an expression against.
    /// </summary>
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/styling/expressions", Name = "MatchName")]
    public class MatchNameExpression : TextExpression
    {
        public MatchNameExpression(string value) : base(value)
        {
        }

        public override object Resolve(ExpressionContext context)
        {
            if (context.ContainsKey("materialname"))
            {                
                if (context["materialname"].ToString() == value)
                    return value;
            }
            return null;
        }

        public override string ToString()
        {
            return "materialname:" + value;
        }
    }
}