using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Netherlands3D.LayerStyles
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/styling", Name = "LayerStyle")]
    public class LayerStyle
    {
        public Metadata Metadata { get; }

        public List<StylingRule> StylingRules { get; } = new();
    }
}