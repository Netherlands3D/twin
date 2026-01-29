using Netherlands3D.Coordinates;
using Netherlands3D.LayerStyles;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Styling")]
    public class StylingPropertyData : LayerPropertyData
    {
        public const string NameOfDefaultStyle = "default";

        [DataMember] private string styleName = NameOfDefaultStyle;
        
        [JsonIgnore] public string StyleName => styleName;
        
        [JsonIgnore] public Dictionary<object, LayerFeature> LayerFeatures { get; private set; } = new();
        
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
        
        // /// <summary>
        // /// Every layer has a default symbolizer, drawn from the default style, that can be queried for the appropriate
        // /// properties.
        // /// </summary>
         [JsonIgnore] public Symbolizer DefaultSymbolizer => StylingRules[NameOfDefaultStyle].Symbolizer;


        [JsonIgnore] public readonly UnityEvent OnStylingChanged = new();
        [JsonIgnore] public readonly UnityEvent<string> ColorTypeChanged = new();

        public StylingPropertyData()
        {   
        }
        
        public void SetStylingRule(string stylingRuleName, StylingRule stylingRule)
        {
            StylingRules[stylingRuleName] = stylingRule;
            OnStylingChanged.Invoke();
        }

        public LayerFeature GetLayerFeatureByGeometry(object geometry)
        {
            LayerFeatures.TryGetValue(geometry, out var feature);
            return feature;
        }
        
    }
}