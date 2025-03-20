using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Netherlands3D.LayerStyles.Expressions
{
    /// <summary>
    /// A literal piece of text.
    ///
    /// This is used in places - such as the ExpressionContext - where you need a literal piece of text to resolve
    /// an expression against.
    /// </summary>
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/styling/expressions", Name = "Array")]
    public class ArrayLiteralExpression : LiteralExpression, IEnumerable
    {
        [DataMember(Name = "values")] private LiteralExpression[] values;

        public ArrayLiteralExpression(IEnumerable<LiteralExpression> values)
        {
            this.values = values.ToArray();
        }

        public override object Resolve(ExpressionContext context)
        {
            return values;
        }

        public override string ToString()
        {
            var strings = string.Join(",", values.Select(value => value.ToString()));
            
            return $"(${strings})";
        }

        public IEnumerator GetEnumerator()
        {
            return values.GetEnumerator();
        }
    }
}