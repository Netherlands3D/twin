using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Netherlands3D.LayerStyles;
using Netherlands3D.SerializableGisExpressions;
using Netherlands3D.Timeline;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Transform")]
    public class TimelineStatusStylingLayerPropertyData : StylingPropertyData
    {
        public const string TimelineAttributeIdentifier = "data-timeline-color";
        public const string TimelineColorIdentifier = "timeline-color";

        private Color defaultColor;
        // [JsonIgnore] public List<TimestampCollection> TimestampCollections = new();

        public TimelineStatusStylingLayerPropertyData(Color defaultColor) : base()
        {
            this.defaultColor = defaultColor;
            // StylingRules.Remove(DefaultRuleName);
            StylingRules[DefaultRuleName].Symbolizer.SetFillColor(defaultColor);
            StylingRules[DefaultRuleName].Symbolizer.SetStrokeColor(defaultColor);
        }

        private string GetStylingRuleKey(Timestamp timestamp)
        {
            return GetStylingRuleKey(timestamp.value);
        }
        
        private string GetStylingRuleKey(string status)
        {
            return $"feature.{status}.{TimelineColorIdentifier}";
        }

        public Color? GetColorForTimestamp(Timestamp timestamp)
        {
            if (timestamp == null)
                return null;

            var stylingRuleKey = GetStylingRuleKey(timestamp);
            if (StylingRules.TryGetValue(stylingRuleKey, out var rule))
            {
                return rule.Symbolizer.GetColor(colorType);
            }

            return null;
        }

        public IList GetStates()
        {
            var list = new List<string>(StylingRules.Count);
            int i = 0;
            foreach (var stylingRuleKey in StylingRules.Keys)
            {
                Debug.Log(i+"\t"+ stylingRuleKey);
                i++;
                list.Add(GetStylingRuleName(stylingRuleKey));
            }
            return list; //todo: is this always the same order?
        }

        public Color? GetColorForStatus(string status)
        {
            var stylingRuleKey = GetStylingRuleKey(status);
            return StylingRules[stylingRuleKey].Symbolizer.GetColor(colorType);
        }

        public void SetColorForStatus(string status, Color color)
        {
            var stylingRuleKey = GetStylingRuleKey(status);
            StylingRules[stylingRuleKey].Symbolizer.SetColor(colorType, color);
            OnStylingChanged.Invoke();
        }
        
        public void AddRulesForStatuses(Dictionary<string, Color> stateColors)
        {
            var rules = new Dictionary<string, StylingRule>();
            foreach (var kvp in stateColors)
            {
                var stylingRuleKey = GetStylingRuleKey(kvp.Key);
                var stylingRule = new StylingRule(
                    kvp.Key,
                    Expression.EqualTo(
                        Expression.Get(TimelineAttributeIdentifier),
                        kvp.Key
                    )
                );
                stylingRule.Symbolizer.SetColor(colorType, kvp.Value);
                rules.Add(stylingRuleKey, stylingRule);
            }

            SetStylingRules(rules);
        }
    }
}