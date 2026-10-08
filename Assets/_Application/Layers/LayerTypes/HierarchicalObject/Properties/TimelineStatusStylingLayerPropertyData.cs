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
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "TimelineStatus")]
    public class TimelineStatusStylingLayerPropertyData : StylingPropertyData
    {
        public const string TimelineStatusAttributeIdentifier = "data-timeline-status-color";
        private const string TimelineStatusColorIdentifier = "timeline-status-color";
        
        public TimelineStatusStylingLayerPropertyData()
        {
            StylingRules.Remove(DefaultRuleName); //we do not need this here
        }

        private string GetStylingRuleKey(Timestamp timestamp)
        {
            return GetStylingRuleKey(timestamp.value);
        }
        
        private string GetStylingRuleKey(string status)
        {
            return $"feature.{status}.{TimelineStatusColorIdentifier}";
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
            var stylingRuleKey = GetStylingRuleKey(status);
            return StylingRules[stylingRuleKey].Symbolizer.GetColor(colorType);
        }

        public void SetColorForStatus(string status, Color color)
        {
            var stylingRuleKey = GetStylingRuleKey(status);
            StylingRules[stylingRuleKey].Symbolizer.SetColor(colorType, color);
            OnStylingChanged.Invoke();
        }
        
        public void AddNewRulesForStatuses(Dictionary<string, Color> stateColors)
        {
            var rules = new Dictionary<string, StylingRule>();
            foreach (var kvp in stateColors)
            {
                var stylingRuleKey = GetStylingRuleKey(kvp.Key);
                if(StylingRules.ContainsKey(stylingRuleKey))
                    continue; //rule already exists, do not overwrite the saved color
                
                var stylingRule = new StylingRule(
                    kvp.Key,
                    Expression.EqualTo(
                        Expression.Get(TimelineStatusAttributeIdentifier),
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