using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Netherlands3D.LayerStyles
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/styling", Name = "StylingRule")]
    public class StylingRule
    {
        [DataMember(Name = "name")] public string Name { get; private set; }
        [DataMember(Name = "symbolizer")] public Symbolizer Symbolizer { get; } = new();

        [DataMember(Name = "selector")] public Expression Selector { get; private set; }

        [JsonConstructor]
        private StylingRule()
        {
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