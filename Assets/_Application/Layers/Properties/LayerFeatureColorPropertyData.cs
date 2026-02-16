using System.Runtime.Serialization;
using Netherlands3D.LayerStyles;
using Netherlands3D.SerializableGisExpressions;
using Newtonsoft.Json;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "LayerFeatureColorData")]
    public class LayerFeatureColorPropertyData : StylingPropertyData
    {
        public const string MaterialIndexKey = "data-materialindex";
        public const string MaterialNameIdentifier = "data-materialname";
        public const string ColoringIdentifier = "colorize";
        
        public void SetColor(LayerFeature layerFeature, Color color)
        {
            if (layerFeature.Geometry is not Material mat) return;
            
            int.TryParse(layerFeature.Attributes[MaterialIndexKey], out int materialIndexIdentifier);

            SetColorByMaterialIndex(materialIndexIdentifier, mat.name, color);
        }
        
        public void SetColorByMaterialIndex(int index, string name, Color color)
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
            stylingRule.Symbolizer.SetFillColor(color);

            SetStylingRule(stylingRuleKey, stylingRule);
        }
        
        public Color? GetColor(LayerFeature layerFeature)
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
            return stylingRule.Symbolizer.GetFillColor();
        }
        
        public Color? GetColorByMaterialIndex(int index)
        {
            var stylingRuleKey = ColorizationStyleRuleKey(index);
            if (!StylingRules.TryGetValue(stylingRuleKey, out var stylingRule))
            {
                return null;
            }
            return stylingRule.Symbolizer.GetFillColor();
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
        public LayerFeatureColorPropertyData()
        {
            
        }
    }
}
