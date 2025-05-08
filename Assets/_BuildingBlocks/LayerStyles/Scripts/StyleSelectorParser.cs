using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D
{
    public class StyleSelectorParser
    {
        //todo make this a service
        public static StyleSelectorParser Instance
        {
            get
            {
                if(instance == null)
                {
                    instance = new StyleSelectorParser();
                }
                return instance;
            } 
        }

        private static StyleSelectorParser instance;

        public bool Resolve(string selector, Dictionary<string, string> expressions)
        {
            return true;
        }

        public Symbolizer ResolveSymbologyForFeature(Symbolizer symbolizer, Dictionary<string, LayerStyle> styles, LayerFeature feature)
        {
            foreach (var style in styles.Values)
            {             
                foreach (var rule in style.StylingRules.Values)
                {
                    // if the rule's selector does not match the given attributes - then this symbology does not apply
                    if (!Resolve(rule.Selector, feature.Attributes))
                        continue;

                    Symbolizer.Merge(symbolizer, rule.Symbolizer);
                }
            }
            return symbolizer;
        }
    }
}
