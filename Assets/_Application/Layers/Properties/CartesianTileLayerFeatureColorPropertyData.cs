using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.LayerStyles;
using Netherlands3D.SerializableGisExpressions;
using Newtonsoft.Json;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "LayerFeatureColorData")]
    public class CartesianTileLayerFeatureColorPropertyData : StylingPropertyData
    {
        public const string MaterialIndexKey = "data-materialindex";
        public const string MaterialNameIdentifier = "data-materialname";
        public const string ColoringIdentifier = "colorize";
        
        private Dictionary<string, StylingRule> stylingRuleKeys = new();

        public struct ColorData
        {
            public int index;
            public string name;
            public Color color;
        }
        
        public void SetColor(StylingFeature stylingFeature, Color color, string colorPropertyType)
        {
            int.TryParse(stylingFeature.Attributes[MaterialIndexKey], out int materialIndexIdentifier);
            stylingFeature.Attributes.TryGetValue(MaterialNameIdentifier, out string materialName);

            SetColorByMaterialIndex(materialIndexIdentifier, materialName, color, colorPropertyType);
        }
        
        public void SetColorByMaterialIndex(int index, string name, Color color, string colorPropertyType)
        {
            var stylingRuleName = name;
            var stylingRuleKey = ColorizationStyleRuleKey(index);

            // Add or set the colorization of this feature by its material index
            var stylingRule = new StylingRule(
                stylingRuleName,
                Expression.EqualTo(
                    Expression.Get(MaterialIndexKey),
                    index.ToString()
                )
            );
            SetColorByColorPropertyType(color, stylingRule, colorPropertyType);

            SetStylingRule(stylingRuleKey, stylingRule);
        }
        
        public void SetColorsByMaterialIndices(List<ColorData> colors)
        {
            stylingRuleKeys.Clear();
            foreach (var colorData in colors)
            {
                var stylingRuleKey = ColorizationStyleRuleKey(colorData.index);
                // Add or set the colorization of this feature by its material index
                var stylingRule = StylingRules[stylingRuleKey];
                //todo whenever the ui changes we should be able to choose which color property type here to use
                var colorPropertyType = GetColorPropertyTypeForStylingRule(stylingRuleKey);
                SetColorByColorPropertyType(colorData.color, stylingRule, colorPropertyType);
                stylingRuleKeys.Add(stylingRuleKey, stylingRule);
            }
            SetStylingRules(stylingRuleKeys);
        }
        
        public void SetVisibilityByMaterialIndices(Dictionary<int, bool> newValues)
        {
            stylingRuleKeys.Clear();
            foreach (var kvp in newValues)
            {
                var stylingRuleKey = ColorizationStyleRuleKey(kvp.Key);
                // Add or set the colorization of this feature by its material index
                var stylingRule = StylingRules[stylingRuleKey];
                //todo whenever the ui changes we should be able to choose which color property type here to use
                stylingRule.Symbolizer.SetVisibility(kvp.Value);
                stylingRuleKeys.Add(stylingRuleKey, stylingRule);
            }
            SetStylingRules(stylingRuleKeys);
        }
        
        public Color? GetColorByStylingRuleKey(string stylingRuleKey, string colorPropertyType)
        {
            if (!StylingRules.TryGetValue(stylingRuleKey, out var stylingRule))
            {
                return null;
            }
            return GetColorByColorPropertyType(stylingRule, colorPropertyType);
        }
        
        public bool? GetVisibilityByStylingRuleKey(string stylingRuleKey)
        {
            if (!StylingRules.TryGetValue(stylingRuleKey, out var stylingRule))
            {
                return null;
            }

            return stylingRule.Symbolizer.GetVisibility();
        }
        
        public void RemoveColorForMaterialIndex(int index)
        {
            var stylingRuleKey = ColorizationStyleRuleKey(index);
            RemoveStylingRule(stylingRuleKey);
        }
        
        private string ColorizationStyleRuleKey(int materialIndexIdentifier)
        {
            return $"feature.{materialIndexIdentifier}.{ColoringIdentifier}";
        }

        public string GetStylingRuleNameByMaterialIndex(int materialIndexIdentifier)
        {
            string key = ColorizationStyleRuleKey(materialIndexIdentifier);
            return GetStylingRuleName(key);
        }
        
        public int GetMaterialIndexFromStyleRuleKey(string styleRuleKey)
        {
            int startIndex = styleRuleKey.IndexOf('.') + 1;
            int endIndex = styleRuleKey.LastIndexOf('.');
            if (startIndex > 0 && endIndex > startIndex)
            {
                int index;
                string key = styleRuleKey.Substring(startIndex, endIndex - startIndex);
                int.TryParse(key, out index);
                return index;
            }
            return -1;
        }
        
        [JsonConstructor]
        public CartesianTileLayerFeatureColorPropertyData()
        {
            
        }

        public override List<string> GetUsedColorTypes()
        {
            var keys = new List<string>();
            
            foreach(KeyValuePair<string, StylingRule> kv in StylingRules)
            {
                if(kv.Key.Contains(ColoringIdentifier))
                {
                    keys.Add(kv.Key);
                }
            }
            return keys;
        }

        public string GetColorPropertyTypeForStylingRule(string stylingRuleKey)
        {
            if (StylingRules.TryGetValue(stylingRuleKey, out var stylingRule))
            {
                Color? color = stylingRule.Symbolizer.GetStrokeColor();
                if(color.HasValue)
                    return Symbolizer.StrokeColorProperty;
                
                color = stylingRule.Symbolizer.GetFillColor();
                if(color.HasValue)
                    return Symbolizer.FillColorProperty;
         
            }
            return null;
        }
        
        private void SetColorByColorPropertyType(Color color, StylingRule stylingRule, string colorPropertyType)
        {
            switch (colorPropertyType)
            {
                case Symbolizer.FillColorProperty:
                    stylingRule.Symbolizer.SetFillColor(color);
                    break;
                case Symbolizer.StrokeColorProperty:
                    stylingRule.Symbolizer.SetStrokeColor(color);
                    break;
                default:
                    stylingRule.Symbolizer.SetFillColor(color);
                    break;
            }
        }

        private Color? GetColorByColorPropertyType(StylingRule stylingRule, string colorPropertyType)
        {
            switch (colorPropertyType)
            {
                case Symbolizer.FillColorProperty:
                    return stylingRule.Symbolizer.GetFillColor();
                case Symbolizer.StrokeColorProperty:
                    return stylingRule.Symbolizer.GetStrokeColor();
                default:
                    return stylingRule.Symbolizer.GetFillColor();
            }
        }
    }
}
