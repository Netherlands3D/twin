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
    public class TimelineStylingLayerPropertyData : StylingPropertyData
    {
        public const string TimelineAttributeIdentifier = "data-timeline-color";
        public const string TimelineColorIdentifier = "timeline-color";

        private Color defaultColor;
        private Dictionary<string, Color> colors = new();
        public Dictionary<string, Color> Colors => colors;
        // public UnityEvent<string, Color> OnColorInterpretationChanged = new();
        
        [JsonIgnore] public List<TimestampCollection> TimestampCollections = new();
        [JsonIgnore] public UnityEvent<TimestampCollection> OnTimestampCollectionAdded = new();

        public TimelineStylingLayerPropertyData(Color defaultColor) : base()
        {
            // Interpreter.OnColorInterpretationChanged.AddListener(UpdateRule);
            this.defaultColor = defaultColor;
        }
        
        public void SetDefaultColor(Color defaultColor)
        {
            this.defaultColor = defaultColor;
        }
        
        public void ProcessNewCollection(TimestampCollection newTimestampCollection)
        {
            foreach (var timestamp in newTimestampCollection.Timestamps)
            {
                colors.TryAdd(timestamp.value, defaultColor);
            }
        }
        
        public Color? GetColorForTimestamp(Timestamp timestamp)
        {
            if(timestamp ==null)
                return null;
            
            if (colors.TryGetValue(timestamp.value, out var color))
            {
                return color;
            }

            return null;
        }

        public IList GetStates()
        {
            return colors.Keys.ToList(); //todo: is this always the same order?
        }

        public Color GetColorForStatus(string status)
        {
            return colors[status];
        }

        public void SetColorForStatus(string status, Color color)
        {
            colors[status] = color;
            UpdateRule(status, color);
        }

        private void UpdateRule(string status, Color color)
        {
            var stylingRuleKey = $"feature.{status}.{TimelineColorIdentifier}";
            StylingRules[stylingRuleKey].Symbolizer.SetColor(colorType, color);
            OnStylingChanged.Invoke();
        }

        public void SetColorForFeatureById(string featureId, string colorType, Color? color)
        {
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

        public void AddTimestampCollection(TimestampCollection collection)
        {
            TimestampCollections.Add(collection);
            OnTimestampCollectionAdded.Invoke(collection);
        }

        public void SetRulesForStatuses(Dictionary<string, Color> stateColors)
        {
            var rules = new Dictionary<string, StylingRule>();
            foreach (var kvp in stateColors)
            {
                var stylingRuleKey = $"feature.{kvp.Key}.{TimelineColorIdentifier}";
                var stylingRule = new StylingRule(
                    kvp.Key,
                    Expression.EqualTo(
                        Expression.Get(TimelineAttributeIdentifier),
                        kvp.Key
                    )
                );
                stylingRule.Symbolizer.SetColor(colorType, kvp.Value);
                rules.Add(stylingRuleKey, stylingRule);
                Debug.Log("setting " + stylingRuleKey + " color to: " + kvp.Value);
            }

            SetStylingRules(rules);
        }
    }
}