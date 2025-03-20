using System.Runtime.Serialization;

namespace Netherlands3D.LayerStyles.Expressions
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/styling/expressions", Name = "Bool")]
    public class BoolExpression : LiteralExpression
    {
        [DataMember(Name = "value")] private bool value;

        public BoolExpression(bool value)
        {
            this.value = value;
        }

        public static BoolExpression True() => new(true);
        public static BoolExpression False() => new(false);

        public override object Resolve(ExpressionContext context)
        {
            return value;
        }

        public override string ToString()
        {
            return this.value ? "true" : "false";
        }
    }
}