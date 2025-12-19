using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Coordinates;
using Netherlands3D.LayerStyles;
using Netherlands3D.SerializableGisExpressions;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "LayerFeatureColorData")]
    public class LayerFeatureColorPropertyData : StylingPropertyData
    {
        public const string MaterialIndexIdentifier = "data-materialindex";
        public const string MaterialNameIdentifier = "data-materialname";
        public const string ColoringIdentifier = "colorize";
        
        public void SetColor(LayerFeature layerFeature, Color color)
        {
            int.TryParse(layerFeature.Attributes[MaterialIndexIdentifier], out int materialIndexIdentifier);

            var stylingRuleName = ColorizationStyleRuleName(materialIndexIdentifier);

            // Add or set the colorization of this feature by its material index
            var stylingRule = new StylingRule(
                stylingRuleName,
                Expression.EqualTo(
                    Expression.Get(MaterialIndexIdentifier),
                    materialIndexIdentifier.ToString()
                )
            );
            stylingRule.Symbolizer.SetFillColor(color);

            SetStylingRule(stylingRuleName, stylingRule);
        }
        
        public Color? GetColor(LayerFeature layerFeature)
        {
            if (layerFeature.Geometry is not Material mat) return null;

            int.TryParse(layerFeature.GetAttribute(MaterialIndexIdentifier), out int materialIndexIdentifier);
            var stylingRuleName = ColorizationStyleRuleName(materialIndexIdentifier);

            if (!StylingRules.TryGetValue(stylingRuleName, out var stylingRule))
            {
                if(mat.HasProperty("_Color") || mat.HasProperty("_BaseColor")) //TODO check a list of standardized tags for color properties
                    return mat.color;
                else
                    return null;
            }
            return stylingRule.Symbolizer.GetFillColor();
        }
        
        private string ColorizationStyleRuleName(int materialIndexIdentifier)
        {
            return $"feature.{materialIndexIdentifier}.{ColoringIdentifier}";
        }    
        
        [JsonConstructor]
        public LayerFeatureColorPropertyData()
        {
            
        }
    }
}
