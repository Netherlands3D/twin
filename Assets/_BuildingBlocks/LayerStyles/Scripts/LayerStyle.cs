using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Netherlands3D.LayerStyles
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/styling", Name = "LayerStyle")]
    public class LayerStyle
    {
        [DataMember(Name = "metadata")] public Metadata Metadata { get; } = new();

        [DataMember(Name = "stylingRules")] public Dictionary<string, StylingRule> StylingRules { get; } = new()
        {
            { "default", new StylingRule("default") }
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

        /// <summary>
        /// Empty constructor needed to prevent name from clearing, when you create a layer style from code
        /// we want to require you to immediately add the name because this is needed in the dictionary lookups; but the
        /// JSON Constructor should _not_ have it because the name is not an immediate member
        /// </summary>
        [JsonConstructor]
        private LayerStyle()
        {
        }

        public LayerStyle(string name)
        {
            Metadata.Name = name;
        }
    }
}