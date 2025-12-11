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
        

        [DataMember] private string activeToolProperty = Symbolizer.FillColorProperty; //default

        [JsonIgnore]
        public string ActiveToolProperty
        {
            get => activeToolProperty;
            set
            {
                activeToolProperty = value;
                ToolPropertyChanged.Invoke(value);
            }
        }
        
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
        // public static LayerStyle CreateDefaultStyle()
        // {
        //     return new LayerStyle(DefaultRuleName);
        // }

        // /// <summary>
        // /// A list of styles with their names (which are meant as machine-readable names and not human-readable names,
        // /// for the latter the 'title' field exists), including a default style that always applies.
        // /// </summary>
        // [SerializeField, DataMember]
        // protected Dictionary<string, LayerStyle> styles; 
        //
        // [JsonIgnore] public Dictionary<string, LayerStyle> Styles => styles;
        //
        //
        // /// <summary>
        // /// Every layer has a default style, this is a style that applies to all objects and features in this
        // /// layer without any conditions.
        // /// </summary>
        // [JsonIgnore] public LayerStyle DefaultStyle => Styles[NameOfDefaultStyle];
        //
        // /// <summary>
        // /// Every layer has a default symbolizer, drawn from the default style, that can be queried for the appropriate
        // /// properties.
        // /// </summary>
         [JsonIgnore] public Symbolizer DefaultSymbolizer => StylingRules[NameOfDefaultStyle].Symbolizer;


        [JsonIgnore] public readonly UnityEvent OnStylingChanged = new();
        // [JsonIgnore] public readonly UnityEvent<LayerStyle> StyleAdded = new();
        // [JsonIgnore] public readonly UnityEvent<LayerStyle> StyleRemoved = new();
        [JsonIgnore] public readonly UnityEvent<string> ToolPropertyChanged = new();

        public StylingPropertyData()
        {            
            // styles = new Dictionary<string, LayerStyle>
            // {
            //     { NameOfDefaultStyle, LayerStyle.CreateDefaultStyle() }
            // };
        }
        //
        // [JsonConstructor]
        // public StylingPropertyData(Dictionary<string, LayerStyle> styles)
        // {
        //     // this.styles = styles ?? new Dictionary<string, LayerStyle>
        //     // {
        //     //     { NameOfDefaultStyle, LayerStyle.CreateDefaultStyle() }
        //     // };
        // }
        //
        // public void AddStyle(LayerStyle style)
        // {
        //     if (Styles.TryAdd(style.Metadata.Name, style))
        //     {
        //         StyleAdded.Invoke(style);
        //         OnStylingChanged.Invoke();
        //     }
        // }
        //
        // public void RemoveStyle(LayerStyle style)
        // {
        //     if (Styles.Remove(style.Metadata.Name))
        //     {
        //         StyleRemoved.Invoke(style);
        //     }
        // }


// #if UNITY_EDITOR
//         [SerializeField]
//         private List<LayerStyle> styles_editor = new(); // always serialized
//         public List<LayerStyle> GetEditorStyles()
//         {
//             if (styles_editor == null || styles_editor.Count == 0)
//             {
//                 styles_editor = styles?.Values.ToList() ?? new() { LayerStyle.CreateDefaultStyle() };
//             }
//             return styles_editor;
//         }
// #endif
        public void SetStylingRule(string stylingRuleName, StylingRule stylingRule)
        {
            StylingRules[stylingRuleName] = stylingRule;
            OnStylingChanged.Invoke();
        }

        public void SetDefaultSymbolizerColor(Color? color)
        {
            var symbolizer = AnyFeature.Symbolizer;
            symbolizer.SetColor(ActiveToolProperty, color);
            OnStylingChanged.Invoke();
        }
        
        public Color? GetDefaultSymbolizerColor()
        {
            var symbolizer = AnyFeature.Symbolizer;
            return symbolizer.GetColor(ActiveToolProperty);
        }
    }
}