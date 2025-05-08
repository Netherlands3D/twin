using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Netherlands3D.LayerStyles
{
    /// <summary>
    /// A Styling Rule is a set of styling instructions -symbology- that happens when a given Selector is matched.
    ///
    /// A matching analogy is a style rule in CSS (see https://drafts.csswg.org/cssom/#the-cssstylerule-interface),
    /// for example:
    /// 
    /// ```css
    /// .className .childElementClassName {
    ///     color: black;
    /// }
    /// ```
    ///
    /// In the example above, the styling rule is the entire block with a selector (`.className .childElementClassName`)
    /// that provides an expression which elements -in our case features- will have the styles in that block applied to
    /// them. In our situation, the style block is expressed by the Symbolizer and can have methods such as
    /// getFillColor, depending on which extension classes you include.
    ///
    /// Styling rules are contained in a "Style" object that is best seen analoguous to a stylesheet file in CSS; using
    /// this structure it is possible to create a styling scheme with which to best describe what a layer should look
    /// like.
    ///
    /// For more information, including the terminology, please see https://docs.ogc.org/DRAFTS/18-067r4.html. 
    /// </summary>
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/styling", Name = "StylingRule")]
    public class StylingRule
    {
        /// <summary>
        /// A human-readable name to clarify the purpose of this styling rule with.
        /// </summary>
        [DataMember(Name = "name")] public string Name { get; private set; }
        
        /// <summary>
        /// Instructions, such as fill-color, are managed though the symbolizer and supporting extension classes
        /// matching "Requirement Classes" matching types of properties in
        /// https://docs.ogc.org/DRAFTS/18-067r4.html#overview.
        /// </summary>
        [DataMember(Name = "symbolizer")] public Symbolizer Symbolizer { get; } = new();

        /// <summary>
        /// An expression whether a feature should match this styling rule, matching is done using an expression as
        /// described in https://docs.ogc.org/DRAFTS/18-067r4.html#_expressions.
        /// </summary>
        [DataMember(Name = "selector")] public string Selector { get; private set; }

        [JsonConstructor]
        private StylingRule()
        {
        }

        public StylingRule(string name)
        {
            Name = name;
            
            // Applies always - the selector will always return true
            Selector = "true";
        }

        public StylingRule(string name, string selector)
        {
            Name = name;
            Selector = selector;
        }
    }
}