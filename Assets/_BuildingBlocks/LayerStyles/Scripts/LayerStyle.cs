using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Twin.Layers;
using Newtonsoft.Json;
using UnityEngine;

namespace Netherlands3D.LayerStyles
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/styling", Name = "LayerStyle")]
    public class LayerStyle
    {
        private const string DefaultRuleName = "default";
        
        [DataMember(Name = "metadata")] public Metadata Metadata { get; } = new();

        [DataMember(Name = "stylingRules")] public Dictionary<string, StylingRule> StylingRules { get; } = new()
        {
            { DefaultRuleName, new StylingRule(DefaultRuleName) }
        };
        
        /// <summary>
        /// The default rule - or the one that is applied to all features inside this layer - this stylingrule
        /// is not expected to have any expression associated with it so that a shorthand is available to apply styling
        /// to all elements in this style,
        /// </summary>
        public StylingRule AnyFeature => StylingRules[DefaultRuleName];

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
            return new LayerStyle(DefaultRuleName);
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

        /// <summary>
        /// Attempts to combine the symbology of all matching rules into one Symbolizer so that a calling method
        /// does not care about cascading rules or how the values are determined; that is handled in this function.
        ///
        /// The provided Symbolizer is a new instance where all matching symbology rules are merged into one, by
        /// pre-creating it in the caller we can achieve immutability with just one instantiation.
        /// </summary>
        /// <returns></returns>
        public Symbolizer CollectSymbologyFor<T>(Symbolizer symbolizer, LayerFeature<T> feature) where T : Component
        {
            foreach (var rule in StylingRules)
            {
                symbolizer = rule.Value.CollectSymbologyFor(symbolizer, feature);
            }
            
            return symbolizer;
        }
    }
}