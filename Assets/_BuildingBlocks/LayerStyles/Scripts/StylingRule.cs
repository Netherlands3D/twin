using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Netherlands3D.LayerStyles
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/styling", Name = "StylingRule")]
    public class StylingRule
    {
        [DataMember] public string Name { get; private set; }
        [DataMember] public Symbolizer Symbolizer { get; } = new();

        [DataMember] public Expression Selector { get; private set; }

        [JsonConstructor]
        private StylingRule()
        {
            Name = "";
        }

        public StylingRule(string name)
        {
            Name = name;
            Selector = BoolExpression.True();
        }

        public StylingRule(string name, Expression selector)
        {
            Name = name;
            Selector = selector;
        }
    }
}