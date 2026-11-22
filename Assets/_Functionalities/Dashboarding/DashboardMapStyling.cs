using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.CityJson.Visualisation;
using Netherlands3D.LayerStyles;
using Netherlands3D.SerializableGisExpressions;
using Netherlands3D.Twin.DataSets;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.DataSets
{
    internal enum DashboardMapLayer
    {
        Base,
        Segment,
        Dwellings,
        Phase
    }

    /// <summary>
    /// Applies dashboard colors through temporary NL3D styling rules without changing saved layer styles.
    /// </summary>
    internal sealed class DashboardMapStyling : IDisposable
    {
        private const string ColorAttribute = "data-dashboard-color";
        private readonly DashboardObjectBinding binding;
        private IReadOnlyDictionary<string, Color> colors;
        private DashboardMapLayer selectedLayer;
        private readonly Dictionary<CityJSONLayerGameObject, StylingPropertyData> styles = new();

        public DashboardMapStyling(DashboardObjectBinding binding)
        {
            this.binding = binding;
        }

        public void Update(DashboardMapLayer layer, IReadOnlyDictionary<string, Color> palette)
        {
            selectedLayer = layer;
            colors = palette;
            Refresh();
        }

        public void Dispose()
        {
            foreach (var visualization in styles.Keys.ToArray())
                Restore(visualization);
        }

        private void Refresh()
        {
            var targets = colors != null ? binding.Visualizations
                .Where(visualization => visualization.CityJson.CityObjects.Any(binding.IsDashboardObject)).ToArray()
                : Array.Empty<CityJSONLayerGameObject>();
            foreach (var visualization in styles.Keys.Except(targets).ToArray())
                Restore(visualization);

            foreach (var visualization in targets)
            {
                if (!styles.TryGetValue(visualization, out var style))
                {
                    style = new StylingPropertyData();
                    style.StylingRules.Clear();
                    styles.Add(visualization, style);
                    visualization.RuntimeStyles.Add(style);
                    visualization.OnFeatureAdded.AddListener(OnFeatureAdded);
                }

                style.StylingRules.Clear();
                foreach (var (key, color) in colors)
                {
                    var rule = new StylingRule(key, Expression.EqualTo(Expression.Get(ColorAttribute), key));
                    rule.Symbolizer.SetFillColor(color);
                    rule.Symbolizer.SetStrokeColor(color);
                    style.StylingRules.Add(key, rule);
                }
                foreach (var feature in visualization.LayerFeatures.Values)
                {
                    if (feature.Geometry is CityObjectVisualizer visualizer)
                        SetColorAttribute(visualizer, feature.Attributes);
                }
                visualization.ApplyStyling();
            }
        }

        private void OnFeatureAdded(CityObjectVisualizer visualizer)
        {
            foreach (var visualization in styles.Keys)
            {
                var feature = visualization.GetLayerFeatureByGeometry(visualizer);
                if (feature == null)
                    continue;

                SetColorAttribute(visualizer, feature.Attributes);
                visualization.ApplyStylingToFeature(feature);
                return;
            }
        }

        private void SetColorAttribute(CityObjectVisualizer visualizer, Dictionary<string, string> attributes)
        {
            if (!binding.IsDashboardObject(visualizer.CityObject))
            {
                attributes.Remove(ColorAttribute);
                return;
            }

            binding.Objects.TryGetValue(visualizer.CityObject, out var block);
            attributes[ColorAttribute] = GetColorKey(block, selectedLayer);
        }

        private void Restore(CityJSONLayerGameObject visualization)
        {
            var style = styles[visualization];
            styles.Remove(visualization);
            if (visualization == null)
                return;

            visualization.OnFeatureAdded.RemoveListener(OnFeatureAdded);
            visualization.RuntimeStyles.Remove(style);
            foreach (var feature in visualization.LayerFeatures.Values)
                feature.Attributes.Remove(ColorAttribute);
            visualization.ApplyStyling();
        }

        internal static string GetColorKey(DashboardObjectData.BlockData block, DashboardMapLayer layer)
        {
            if (block == null || layer == DashboardMapLayer.Base)
                return "base";
            if (layer == DashboardMapLayer.Phase)
                return block.LatestPhase?.ToLowerInvariant() ?? "base";

            var housing = block.HousingSummary;
            if (!housing.Total.CompleteValue.HasValue)
                return "base";
            if (layer == DashboardMapLayer.Dwellings)
                return housing.Total.Value < 300 ? "low" : housing.Total.Value < 600 ? "medium" : "high";
            if (housing.Total.Value == 0)
                return "base";

            var dominant = Enumerable.Range(0, housing.Segments.Length).OrderByDescending(index => housing.Values[index].Value).First();
            return DashboardColors.SegmentKey(housing.Segments[dominant]);
        }
    }
}
