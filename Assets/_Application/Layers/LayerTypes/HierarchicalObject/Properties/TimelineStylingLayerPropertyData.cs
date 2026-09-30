using System;
using System.Collections.Generic;
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

        [JsonIgnore] public TimestampValueStatusInterpreter Interpreter { get; set; } = new TimestampValueStatusInterpreter(Color.white); //todo: use default color from styling

        [JsonIgnore] public List<TimestampCollection> TimestampCollections = new();
        [JsonIgnore] public UnityEvent<TimestampCollection> OnTimestampCollectionAdded = new();

        public TimelineStylingLayerPropertyData() : base()
        {
            Interpreter.OnColorInterpretationChanged.AddListener(UpdateRule);
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