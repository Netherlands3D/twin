using System;
using System.Runtime.Serialization;
using Netherlands3D.LayerStyles;
using Netherlands3D.SerializableGisExpressions;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Transform")]
    public class TimelineStylingLayerPropertyData : StylingPropertyData
    {
        public const string TimelineAttributeIdentifier = "data-timeline-color";
        public const string TimelineColorIdentifier = "timeline-color";

        public void SetColorForFeatureById(string featureId, string colorType, Color? color)
        {
            Debug.Log("setting " + featureId + " " + colorType + " to " + color);
            var stylingRuleKey = $"feature.{featureId}.{TimelineColorIdentifier}";
            var stylingRule = new StylingRule(
                featureId,
                Expression.EqualTo(
                    Expression.Get(TimelineAttributeIdentifier),
                    featureId
                )
            );
            stylingRule.Symbolizer.SetColor(colorType, color);
            SetStylingRule(stylingRuleKey, stylingRule);
        }

        public Color? GetColorForFeatureById(string id, string colorType)
        {
            var stylingRuleKey = $"feature.{id}.{TimelineColorIdentifier}";
            if (!StylingRules.TryGetValue(stylingRuleKey, out var stylingRule))
            {
                return Color.white;
            }

            return stylingRule.Symbolizer.GetColor(colorType);
        }
    }
}