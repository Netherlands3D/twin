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
        
        public void SetColor(LayerFeature layerFeature, Color color, string colorPropertyType)
        {
            if (layerFeature.Geometry is not Material mat) return;
            
            int.TryParse(layerFeature.Attributes[MaterialIndexKey], out int materialIndexIdentifier);
            layerFeature.Attributes.TryGetValue(MaterialNameIdentifier, out string materialName);

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
        
        public void SetColorsByMaterialIndices(List<ColorData> colors, string colorPropertyType)
        {
            stylingRuleKeys.Clear();
            foreach (var colorData in colors)
            {
                var stylingRuleName = colorData.name;
                var stylingRuleKey = ColorizationStyleRuleKey(colorData.index);

                // Add or set the colorization of this feature by its material index
                var stylingRule = new StylingRule(
                    stylingRuleName,
                    Expression.EqualTo(
                        Expression.Get(MaterialIndexKey),
                        colorData.index.ToString()
                    )
                );
                //todo whenever the ui changes we should be able to choose which color property type here to use
                SetColorByColorPropertyType(colorData.color, stylingRule, colorPropertyType);
                stylingRuleKeys.Add(stylingRuleKey, stylingRule);
            }
            SetStylingRules(stylingRuleKeys);
        }
        
        public Color? GetColor(LayerFeature layerFeature, string colorPropertyType)
        {
            if (layerFeature.Geometry is not Material mat) return null;

            int.TryParse(layerFeature.GetAttribute(MaterialIndexKey), out int materialIndexIdentifier);
            var stylingRuleKey = ColorizationStyleRuleKey(materialIndexIdentifier);
            if (!StylingRules.TryGetValue(stylingRuleKey, out var stylingRule))
            {
                if(mat.HasProperty("_Color") || mat.HasProperty("_BaseColor")) //TODO check a list of standardized tags for color properties
                    return mat.color;
                else
                    return null;
            }
            return GetColorByColorPropertyType(stylingRule, colorPropertyType);
        }
        
        public Color? GetColorByStylingRuleKey(string stylingRuleKey, string colorPropertyType)
        {
            if (!StylingRules.TryGetValue(stylingRuleKey, out var stylingRule))
            {
                return null;
            }
            return GetColorByColorPropertyType(stylingRule, colorPropertyType);
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
