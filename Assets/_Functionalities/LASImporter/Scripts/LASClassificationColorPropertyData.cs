using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Netherlands3D.Functionalities.LASImporter.Parsing;
using Netherlands3D.LayerStyles;
using Netherlands3D.SerializableGisExpressions;
using Netherlands3D.Twin.Layers.Properties;
using Newtonsoft.Json;
using UnityEngine;

namespace Netherlands3D.Functionalities.LASImporter
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "LasClassificationColors")]
    public class LASClassificationColorPropertyData : StylingPropertyData
    {
        public const string ClassificationIdKey = "las-classification-id";
        public const string ColoringIdentifier = "classification-color";

        [DataMember(Name = "classificationCounts")]
        private readonly Dictionary<int, int> classificationCounts = new();

        [JsonIgnore]
        public IReadOnlyDictionary<int, int> ClassificationCounts => classificationCounts;

        public LASClassificationColorPropertyData()
        {
        }

        public void EnsureClassification(byte classification, int count, Color color)
        {
            classificationCounts[classification] = count;
            var key = ClassificationStyleRuleKey(classification);
            if (StylingRules.ContainsKey(key))
                return;

            SetColorByClassification(classification, LASClassificationColors.GetName(classification), color);
        }

        public IEnumerable<byte> GetClassifications()
        {
            return classificationCounts.Keys
                .OrderBy(id => id)
                .Select(id => (byte)id);
        }

        public int GetCount(byte classification)
        {
            return classificationCounts.GetValueOrDefault(classification);
        }

        public void SetColorByClassification(byte classification, string name, Color color)
        {
            var rule = new StylingRule(
                name,
                Expression.EqualTo(
                    Expression.Get(ClassificationIdKey),
                    classification.ToString()
                )
            );
            rule.Symbolizer.SetFillColor(color);

            SetStylingRule(ClassificationStyleRuleKey(classification), rule);
        }

        public Color? GetColorByClassification(byte classification)
        {
            var key = ClassificationStyleRuleKey(classification);
            return StylingRules.TryGetValue(key, out var rule)
                ? rule.Symbolizer.GetFillColor()
                : null;
        }

        public string GetClassificationName(byte classification)
        {
            var key = ClassificationStyleRuleKey(classification);
            return StylingRules.TryGetValue(key, out var rule)
                ? rule.Name
                : LASClassificationColors.GetName(classification);
        }

        private static string ClassificationStyleRuleKey(byte classification)
        {
            return $"classification.{classification}.{ColoringIdentifier}";
        }
    }
}
