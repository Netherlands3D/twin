using NUnit.Framework;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Netherlands3D.LayerStyles.Expressions
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/styling/expressions", Name = "MatchIndex")]
    public class MatchIndexExpression : Expression
    {
        [DataMember(Name = "value")] private List<int> value;

        public MatchIndexExpression(List<int> value)
        {
            this.value = value;
        }
      
        public override object Resolve(ExpressionContext context)
        {
            if (context.ContainsKey("materialindex"))
            {
                int index = int.Parse(context["materialindex"].ToString());
                if (value.Contains(index))
                    return value;
            }
            return null;
        }
        
        public override string ToString()
        {
            string indices = "indices_";
            foreach (int i in value)
                indices += i.ToString() + "_";
            return indices;
        }
    }
}