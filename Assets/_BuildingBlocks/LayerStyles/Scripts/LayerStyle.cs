using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Netherlands3D.LayerStyles
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/styling", Name = "LayerStyle")]
    public sealed class LayerStyle
    {
        public Metadata Metadata { get; } = new();

        public List<StylingRule> StylingRules { get; } = new()
        {
            new StylingRule("default")
        };

        /// <summary>
        /// Static factory method to construct a default style with.
        ///
        /// This static factory method will ensure that the LayerStyle class is in control what makes up
        /// for a default layer style, and if there are properties that need to come with that. This way, any
        /// other part of the code that wants a fresh 'default' style can use this method and does not need to know
        /// what that actually means.
        /// </summary>
        /// <returns></returns>
        public static LayerStyle CreateDefaultStyle()
        {
            return new LayerStyle("default");
        }

        public LayerStyle(string name)
        {
            Metadata.Name = name;
        }
    }
}