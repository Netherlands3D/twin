using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.LayerStyles;
using Netherlands3D.SerializableGisExpressions;
using Netherlands3D.Timeline;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "TimelineStatus")]
    public class TimelineStatusStylingLayerPropertyData : StylingPropertyData
    {
        public const string TimelineStatusAttributeIdentifier = "data-timeline-status-color";
        private const string TimelineStatusColorIdentifier = "timeline-status-color";
        
        public TimelineStatusStylingLayerPropertyData()
        {
            StylingRules.Remove(DefaultRuleName); //we do not need this here
        }
        
        private string GetStatusStylingRuleKey(string status)
        {
            return $"feature.{status}.{TimelineStatusColorIdentifier}";
        }
        
        public IList GetStylingRuleNames()
        {
            var list = new List<string>(StylingRules.Count);
            foreach (var kvp in StylingRules)
            {
                list.Add(kvp.Value.Name);
            }
            return list; //todo: is this always the same order?
        }

        public Color? GetColorForStatus(string status)
        {
            var stylingRuleKey = GetStatusStylingRuleKey(status);
            return StylingRules[stylingRuleKey].Symbolizer.GetColor(colorType);
        }
        
        public bool? GetVisibilityForStatus(string status)
        {
            var stylingRuleKey = GetStatusStylingRuleKey(status);
            return StylingRules[stylingRuleKey].Symbolizer.GetVisibility();
        }

        public void SetColorForStatus(string status, Color color)
        {
            var stylingRuleKey = GetStatusStylingRuleKey(status);
            StylingRules[stylingRuleKey].Symbolizer.SetColor(Symbolizer.FillColorProperty, color);
            StylingRules[stylingRuleKey].Symbolizer.SetColor(Symbolizer.StrokeColorProperty, color);
            OnStylingChanged.Invoke();
        }

        public void SetColorsForStatuses(Dictionary<string, Color> stateColors)
        {
            foreach (var kvp in stateColors)
            {
                var stylingRuleKey = GetStatusStylingRuleKey(kvp.Key);
                if (StylingRules.TryGetValue(stylingRuleKey, out var rule))
                {
                    rule.Symbolizer.SetColor(Symbolizer.FillColorProperty, kvp.Value);
                    rule.Symbolizer.SetColor(Symbolizer.StrokeColorProperty, kvp.Value);
                }
            }
            OnStylingChanged.Invoke();
        }
        
        public void AddNewRulesForStatuses(Dictionary<string, Color> stateColors)
        {
            var rules = new Dictionary<string, StylingRule>();
            foreach (var kvp in stateColors)
            {
                var stylingRuleKey = GetStatusStylingRuleKey(kvp.Key);
                if(StylingRules.ContainsKey(stylingRuleKey))
                    continue; //rule already exists, do not overwrite the saved color
                
                var stylingRule = new StylingRule(
                    kvp.Key,
                    Expression.EqualTo(
                        Expression.Get(TimelineStatusAttributeIdentifier),
                        kvp.Key
                    )
                );
                stylingRule.Symbolizer.SetColor(Symbolizer.FillColorProperty, kvp.Value);
                stylingRule.Symbolizer.SetColor(Symbolizer.StrokeColorProperty, kvp.Value);
                stylingRule.Symbolizer.SetVisibility(true);
                rules.Add(stylingRuleKey, stylingRule);
            }

            SetStylingRules(rules);
        }

        public void SetVisibilityForStatuses(Dictionary<string, bool> changedStateVisibility)
        {
            foreach (var kvp in changedStateVisibility)
            {
                var stylingRuleKey = GetStatusStylingRuleKey(kvp.Key);
                if (StylingRules.TryGetValue(stylingRuleKey, out var rule))
                {
                    rule.Symbolizer.SetVisibility(kvp.Value);
                }
            }
            OnStylingChanged.Invoke();
        }
    }
}