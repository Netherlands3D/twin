using System.Runtime.Serialization;

namespace Netherlands3D.LayerStyles.Expressions
{
    /// <summary>
    /// A literal piece of text.
    ///
    /// This is used in places - such as the ExpressionContext - where you need a literal piece of text to resolve
    /// an expression against.
    /// </summary>
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/styling/expressions", Name = "Text")]
    public class TextExpression : LiteralExpression
    {
        [DataMember(Name = "value")] private string value;

        public TextExpression(string value)
        {
            this.value = value;
        }

        public override object Resolve(ExpressionContext context)
        {
            return value;
        }

        public static implicit operator TextExpression(string value)
        {
            return new TextExpression(value);
        }
        
        public override string ToString()
        {
            return this.value;
        }
    }
}