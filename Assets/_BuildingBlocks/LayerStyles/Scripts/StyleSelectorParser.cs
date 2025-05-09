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
            // Comma-separated selectors = OR logic
            var individualSelectors = selector.Split(',');
            foreach (var raw in individualSelectors)
            {
                var trimmed = raw.Trim();
                if (MatchesAllAttributes(trimmed, expressions))
                    return true; // any one match is enough
            }
            return false;
        }

        // Handles multiple [key=value] pairs in one selector = AND logic
        private bool MatchesAllAttributes(string selector, Dictionary<string, string> expressions)
        {
            int pos = 0;
            while (pos < selector.Length)
            {
                if (selector[pos] != '[') return false;

                int end = selector.IndexOf(']', pos);
                if (end == -1) return false;

                var content = selector.Substring(pos + 1, end - pos - 1);
                var pair = content.Split('=');
                if (pair.Length != 2) return false;

                var key = pair[0].Trim();
                var value = pair[1].Trim().Trim('"'); // remove quotes if present

                if (!expressions.TryGetValue(key, out var exprValue) || exprValue != value)
                    return false;

                pos = end + 1;
            }

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
