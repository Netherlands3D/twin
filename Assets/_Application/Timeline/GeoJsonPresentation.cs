using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using Newtonsoft.Json.Linq;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using UnityEngine;

namespace Netherlands3D
{
    internal sealed class GeoJsonWorldLabel
    {
        public Vector3 WorldPosition { get; }
        public string Text { get; }
        public string Tooltip { get; }
        public Color Color { get; }
        public Color TextColor { get; }
        public bool SeparateOverlappingLabels { get; }
        public float Width { get; }

        public GeoJsonWorldLabel(
            Vector3 worldPosition,
            string text,
            string tooltip,
            Color color,
            bool separateOverlappingLabels = false,
            float width = 42f)
        {
            WorldPosition = worldPosition;
            Text = text;
            Tooltip = tooltip;
            Color = color;
            TextColor = color.grayscale > 0.62f ? new Color(0.05f, 0.12f, 0.18f) : Color.white;
            SeparateOverlappingLabels = separateOverlappingLabels;
            Width = width;
        }
    }

    public enum GeoJsonPresentationKind
    {
        None,
        RoadIntervention,
        RouteReference,
        SafetyObservations,
        NoiseMeasurements,
        AirQualityMeasurements
    }

    /// <summary>
    /// Recognises a small set of project datasets by their schema and turns their dense properties into a compact,
    /// selectable visual language. The controller deliberately ignores ordinary GeoJSON files.
    /// </summary>
    internal sealed class GeoJsonPresentation
    {
        private static readonly Color MissingDataColor = new(0.47f, 0.51f, 0.55f, 0.8f);
        private static readonly Color[] IntensityColors =
        {
            new(0.173f, 0.482f, 0.714f),
            new(0.255f, 0.714f, 0.655f),
            new(1.000f, 0.878f, 0.545f),
            new(0.957f, 0.427f, 0.263f),
            new(0.647f, 0.000f, 0.149f)
        };

        private static readonly Color[] RiskColors =
        {
            new(0.180f, 0.620f, 0.365f),
            new(0.525f, 0.765f, 0.365f),
            new(1.000f, 0.820f, 0.275f),
            new(0.945f, 0.470f, 0.220f),
            new(0.720f, 0.110f, 0.110f)
        };

        private readonly GeoJsonLayerGameObject visualization;
        private readonly List<Feature> features = new();
        private readonly HashSet<Feature> indexedFeatures = new(new FeatureReferenceComparer());
        private readonly List<Metric> metrics = new();

        public GeoJsonPresentationKind Kind { get; private set; }
        public bool HasData => Kind != GeoJsonPresentationKind.None;
        public IReadOnlyList<string> MetricNames => metrics.Select(metric => metric.Name).ToList();
        public string SelectedMetricName => SelectedMetric?.Name ?? string.Empty;
        public int FeatureCount => features.Count;

        private Metric SelectedMetric => metrics.Count == 0
            ? null
            : metrics[Mathf.Clamp(selectedMetricIndex, 0, metrics.Count - 1)];

        private int selectedMetricIndex;

        public string Title => Kind switch
        {
            GeoJsonPresentationKind.RoadIntervention => "Traffic before & after",
            GeoJsonPresentationKind.RouteReference => "Route geometry",
            GeoJsonPresentationKind.SafetyObservations => "Perceived road safety",
            GeoJsonPresentationKind.NoiseMeasurements => "Noise exposure",
            GeoJsonPresentationKind.AirQualityMeasurements => "NO₂ concentration",
            _ => "GeoJSON view"
        };

        public string ContextLabel => Kind switch
        {
            GeoJsonPresentationKind.RoadIntervention => GetRoadContext(),
            GeoJsonPresentationKind.RouteReference => "Source routes · colour by route ID",
            GeoJsonPresentationKind.SafetyObservations => "Kaleidoscope · responses per intersection",
            GeoJsonPresentationKind.NoiseMeasurements => "Observation points · noise level by time of day",
            GeoJsonPresentationKind.AirQualityMeasurements => GetAirQualityContext(),
            _ => string.Empty
        };

        public string FeatureCaption => Kind switch
        {
            GeoJsonPresentationKind.RoadIntervention => "directions",
            GeoJsonPresentationKind.RouteReference => "line segments",
            GeoJsonPresentationKind.SafetyObservations => "intersections",
            GeoJsonPresentationKind.NoiseMeasurements => "measurement points",
            GeoJsonPresentationKind.AirQualityMeasurements => "measurement points",
            _ => "objects"
        };

        public string LegendTitle => SelectedMetric?.Name ?? string.Empty;

        public string LegendScaleLabel => Kind switch
        {
            GeoJsonPresentationKind.RoadIntervention => "vehicles/day · fixed scenario scale",
            GeoJsonPresentationKind.RouteReference => "route ID · 1–9",
            GeoJsonPresentationKind.SafetyObservations => "weighted average · 1–5",
            GeoJsonPresentationKind.NoiseMeasurements => "dB · shared scale",
            GeoJsonPresentationKind.AirQualityMeasurements => "µg/m³ · shared 2025–2026 scale",
            _ => string.Empty
        };

        public string LegendExplanation
        {
            get
            {
                if (Kind == GeoJsonPresentationKind.RoadIntervention)
                    return "Colour and line width show intensity. Grey means closed or not measured.";
                if (Kind == GeoJsonPresentationKind.RouteReference)
                    return "Colour groups segments with the same route ID. Select a line to view the source data.";
                if (Kind == GeoJsonPresentationKind.NoiseMeasurements)
                    return "Blue is quieter and red is louder. Select a point to view the exact values.";
                if (Kind == GeoJsonPresentationKind.AirQualityMeasurements)
                    return "Blue indicates a lower and red a higher period-average NO₂ concentration.";
                if (Kind == GeoJsonPresentationKind.SafetyObservations)
                    return SelectedMetric?.PositiveIsGood == true
                        ? "Red is negative and green is positive. The colour uses all responses at this intersection."
                        : "Green is calmer and red is busier. The colour uses all responses at this intersection.";
                return string.Empty;
            }
        }

        public GeoJsonPresentation(GeoJsonLayerGameObject visualization)
        {
            this.visualization = visualization;
        }

        public void IndexFeature(Feature feature)
        {
            if (feature?.Properties == null || feature.Geometry == null)
                return;

            if (Kind == GeoJsonPresentationKind.None)
            {
                Kind = DetectKind(feature);
                ConfigureMetrics();
            }

            if (MatchesKind(feature) && indexedFeatures.Add(feature))
                features.Add(feature);
        }

        public void FinalizeIndex()
        {
            if (!HasData || features.Count == 0 || metrics.Count == 0)
                return;

            ApplyStyles();
        }

        public void SelectMetric(int index)
        {
            if (index < 0 || index >= metrics.Count || selectedMetricIndex == index)
                return;

            selectedMetricIndex = index;
            ApplyStyles();
        }

        public float GetLegendValue(float normalized)
        {
            var metric = SelectedMetric;
            return metric == null ? 0f : Mathf.Lerp(metric.Minimum, metric.Maximum, Mathf.Clamp01(normalized));
        }

        public Color GetLegendColor(float normalized)
        {
            var metric = SelectedMetric;
            if (metric == null)
                return MissingDataColor;

            return GetMetricColor(Mathf.Clamp01(normalized), metric);
        }

        public IReadOnlyList<GeoJsonWorldLabel> GetWorldLabels()
        {
            var metric = SelectedMetric;
            if (metric == null)
                return Array.Empty<GeoJsonWorldLabel>();

            return Kind switch
            {
                GeoJsonPresentationKind.RoadIntervention => GetRoadWorldLabels(metric),
                GeoJsonPresentationKind.RouteReference => GetRouteWorldLabels(metric),
                _ => GetPointWorldLabels(metric)
            };
        }

        private void ConfigureMetrics()
        {
            metrics.Clear();
            selectedMetricIndex = 0;

            switch (Kind)
            {
                case GeoJsonPresentationKind.RoadIntervention:
                    metrics.Add(new Metric("Motor vehicles", "count_motor_vehicles", 0f, 4000f));
                    metrics.Add(new Metric("Bicycles", "count_bicycles", 0f, 1000f));
                    break;
                case GeoJsonPresentationKind.RouteReference:
                    metrics.Add(new Metric("Route ID", "id", 1f, 9f));
                    break;
                case GeoJsonPresentationKind.SafetyObservations:
                    metrics.Add(new Metric("This location is safe", "Dit punt is veilig", 1f, 5f, true, true));
                    metrics.Add(new Metric("Safe cycling", "Ik kan hier veilig fietsen", 1f, 5f, true, true));
                    metrics.Add(new Metric("Safe walking", "Ik kan hier veilig wandelen", 1f, 5f, true, true));
                    metrics.Add(new Metric("Traffic volume", "Het is hier druk", 1f, 5f, false, true));
                    metrics.Add(new Metric("Traffic visibility", "Ik zie verkeer goed aankomen", 1f, 5f, true, true));
                    break;
                case GeoJsonPresentationKind.NoiseMeasurements:
                    metrics.Add(new Metric("Day-evening-night (Lden)", "DEN", 35f, 55f));
                    metrics.Add(new Metric("Day (Lday)", "DAY", 35f, 55f));
                    metrics.Add(new Metric("Evening (Levening)", "EVE", 35f, 55f));
                    metrics.Add(new Metric("Night (Lnight)", "NI", 35f, 55f));
                    break;
                case GeoJsonPresentationKind.AirQualityMeasurements:
                    metrics.Add(new Metric("NO₂ concentration", "concentration_no2_ug_m3", 0f, 20f));
                    break;
            }
        }

        private void ApplyStyles()
        {
            var metric = SelectedMetric;
            if (metric == null)
                return;

            var colors = new Dictionary<Feature, Color>();
            var widths = new Dictionary<Feature, float>();
            foreach (var feature in features)
            {
                if (!TryGetMetricValue(feature, metric, out var value))
                {
                    colors[feature] = MissingDataColor;
                    widths[feature] = 0.8f;
                    continue;
                }

                var normalized = Mathf.InverseLerp(metric.Minimum, metric.Maximum, value);
                colors[feature] = GetMetricColor(normalized, metric);
                widths[feature] = Kind == GeoJsonPresentationKind.RouteReference
                    ? 1.05f
                    : Mathf.Lerp(0.9f, 2.4f, Mathf.Sqrt(normalized));
            }

            if (Kind is GeoJsonPresentationKind.RoadIntervention or GeoJsonPresentationKind.RouteReference)
                visualization.SetLineFeatureStyles(colors, widths);
            else
                visualization.SetPointFeatureStyles(colors);
        }

        private IReadOnlyList<GeoJsonWorldLabel> GetRoadWorldLabels(Metric metric)
        {
            var labels = new List<GeoJsonWorldLabel>();
            foreach (var feature in features)
            {
                // Road-intervention files contain one feature and count per carriageway direction. Keep those
                // directional values separate instead of collapsing the pair into one cross-section total.
                if (!TryGetMetricValue(feature, metric, out var value)
                    || !visualization.TryGetFeatureCenter(feature, out var position))
                    continue;

                var normalized = Mathf.InverseLerp(metric.Minimum, metric.Maximum, value);
                var color = GetMetricColor(normalized, metric);
                TryGetString(feature, "street", out var street);
                TryGetString(feature, "direction", out var direction);
                var directionLabel = string.IsNullOrWhiteSpace(direction) ? string.Empty : $" · {direction}";
                labels.Add(new GeoJsonWorldLabel(
                    position,
                    FormatCompactValue(value),
                    $"{street}{directionLabel} · {metric.Name}: {value:0} vehicles/day",
                    color,
                    true));
            }
            return labels;
        }

        private IReadOnlyList<GeoJsonWorldLabel> GetRouteWorldLabels(Metric metric)
        {
            var labels = new List<GeoJsonWorldLabel>();
            foreach (var group in features.GroupBy(feature =>
                         TryGetString(feature, "id", out var routeId) ? routeId : "?"))
            {
                var positions = group
                    .Select(feature => visualization.TryGetFeatureCenter(feature, out var center)
                        ? (Vector3?)center
                        : null)
                    .Where(position => position.HasValue)
                    .Select(position => position.Value)
                    .ToList();
                if (positions.Count == 0 || !float.TryParse(group.Key, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                    continue;

                var position = positions.Aggregate(Vector3.zero, (sum, current) => sum + current) / positions.Count;
                var normalized = Mathf.InverseLerp(metric.Minimum, metric.Maximum, value);
                labels.Add(new GeoJsonWorldLabel(
                    position,
                    $"R{group.Key}",
                    $"Route {group.Key} · {group.Count()} segments",
                    GetMetricColor(normalized, metric)));
            }
            return labels;
        }

        private IReadOnlyList<GeoJsonWorldLabel> GetPointWorldLabels(Metric metric)
        {
            var labels = new List<GeoJsonWorldLabel>();
            foreach (var feature in features)
            {
                if (!TryGetMetricValue(feature, metric, out var value)
                    || !visualization.TryGetFeatureCenter(feature, out var position))
                    continue;

                var normalized = Mathf.InverseLerp(metric.Minimum, metric.Maximum, value);
                var color = GetMetricColor(normalized, metric);
                var valueText = value.ToString("0.0", CultureInfo.InvariantCulture);
                var unit = Kind switch
                {
                    GeoJsonPresentationKind.NoiseMeasurements => " dB",
                    GeoJsonPresentationKind.AirQualityMeasurements => " µg/m³",
                    _ => " / 5"
                };
                var labelText = Kind is GeoJsonPresentationKind.AirQualityMeasurements
                    or GeoJsonPresentationKind.NoiseMeasurements
                    ? valueText + unit
                    : valueText;
                TryGetString(feature, "name", out var name);
                if (string.IsNullOrWhiteSpace(name))
                    TryGetString(feature, "DESC_", out name);
                if (string.IsNullOrWhiteSpace(name))
                    TryGetString(feature, "location_name", out name);
                labels.Add(new GeoJsonWorldLabel(
                    position,
                    labelText,
                    $"{name} · {metric.Name}: {valueText}{unit}",
                    color,
                    width: Kind switch
                    {
                        GeoJsonPresentationKind.AirQualityMeasurements => 86f,
                        GeoJsonPresentationKind.NoiseMeasurements => 62f,
                        _ => 42f
                    }));
            }
            return labels;
        }

        private static string FormatCompactValue(float value)
        {
            if (value >= 1000f)
                return (value / 1000f).ToString("0.#", CultureInfo.InvariantCulture) + "k";
            return value.ToString("0", CultureInfo.InvariantCulture);
        }

        private static Color GetMetricColor(float normalized, Metric metric)
        {
            var palette = metric.UsesSafetyPalette ? RiskColors : IntensityColors;
            var value = metric.UsesSafetyPalette && metric.PositiveIsGood ? 1f - normalized : normalized;
            value = Mathf.Clamp01(value);

            var scaled = value * (palette.Length - 1);
            var lower = Mathf.FloorToInt(scaled);
            var upper = Mathf.Min(lower + 1, palette.Length - 1);
            return Color.Lerp(palette[lower], palette[upper], scaled - lower);
        }

        private bool TryGetMetricValue(Feature feature, Metric metric, out float value)
        {
            if (metric.IsWeightedScore)
                return TryGetWeightedScore(feature, metric.PropertyKey, out value);

            return TryGetFloat(feature, metric.PropertyKey, out value);
        }

        private static bool TryGetWeightedScore(Feature feature, string propertyPrefix, out float value)
        {
            value = 0f;
            if (!TryGetPropertyByPrefix(feature, propertyPrefix, out var rawValue) || rawValue == null)
                return false;

            var weightedTotal = 0f;
            var responseCount = 0f;
            foreach (var item in EnumerateValues(rawValue))
            {
                var text = item?.ToString();
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                var separator = text.LastIndexOf(':');
                if (separator < 0
                    || !float.TryParse(text[(separator + 1)..].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var count)
                    || count <= 0f)
                    continue;

                var answer = text[..separator].Trim();
                var score = answer switch
                {
                    "Volledig mee oneens" => 1,
                    "Oneens" => 2,
                    "Neutraal" => 3,
                    "Eens" => 4,
                    "Volledig mee eens" => 5,
                    _ => 0
                };
                if (score == 0)
                    continue;

                weightedTotal += score * count;
                responseCount += count;
            }

            if (responseCount <= 0f)
                return false;

            value = weightedTotal / responseCount;
            return true;
        }

        private static IEnumerable<object> EnumerateValues(object rawValue)
        {
            if (rawValue is JArray array)
            {
                foreach (var token in array)
                    yield return token.Type == JTokenType.String ? token.Value<string>() : token;
                yield break;
            }

            if (rawValue is IEnumerable enumerable && rawValue is not string)
            {
                foreach (var item in enumerable)
                    yield return item;
                yield break;
            }

            yield return rawValue;
        }

        private string GetRoadContext()
        {
            var firstFeature = features.FirstOrDefault();
            if (firstFeature == null)
                return "Before and after measurement · fixed comparison scale";

            TryGetString(firstFeature, "phase", out var phase);
            TryGetString(firstFeature, "measurement_period", out var period);
            var phaseLabel = string.Equals(phase, "before", StringComparison.OrdinalIgnoreCase)
                ? "Baseline measurement"
                : string.Equals(phase, "after", StringComparison.OrdinalIgnoreCase)
                    ? "Follow-up measurement"
                    : "Measurement";
            return string.IsNullOrWhiteSpace(period) ? phaseLabel : $"{phaseLabel} · {FormatPeriod(period)}";
        }

        private string GetAirQualityContext()
        {
            return "28-day integrated average";
        }

        private static string FormatPeriod(string period)
        {
            if (!DateTime.TryParseExact(period, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return period;

            return date.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        }

        private static GeoJsonPresentationKind DetectKind(Feature feature)
        {
            if (feature.Geometry.Type is GeoJSONObjectType.LineString or GeoJSONObjectType.MultiLineString
                && TryGetString(feature, "dataset_type", out var datasetType)
                && string.Equals(datasetType, "road_intervention_measurement", StringComparison.OrdinalIgnoreCase))
                return GeoJsonPresentationKind.RoadIntervention;

            if (feature.Geometry.Type is GeoJSONObjectType.LineString or GeoJSONObjectType.MultiLineString
                && feature.Properties.ContainsKey("width (m)")
                && TryGetFloat(feature, "id", out _))
                return GeoJsonPresentationKind.RouteReference;

            if (feature.Geometry.Type is GeoJSONObjectType.Point or GeoJSONObjectType.MultiPoint
                && TryGetPropertyByPrefix(feature, "Dit punt is veilig", out _))
                return GeoJsonPresentationKind.SafetyObservations;

            if (feature.Geometry.Type is GeoJSONObjectType.Point or GeoJSONObjectType.MultiPoint
                && TryGetFloat(feature, "DEN", out _)
                && TryGetFloat(feature, "DAY", out _))
                return GeoJsonPresentationKind.NoiseMeasurements;

            if (IsAirQualityFeature(feature))
                return GeoJsonPresentationKind.AirQualityMeasurements;

            return GeoJsonPresentationKind.None;
        }

        private static bool IsAirQualityFeature(Feature feature)
        {
            return feature?.Geometry != null
                   && feature.Geometry.Type is GeoJSONObjectType.Point or GeoJSONObjectType.MultiPoint
                   && TryGetString(feature, "pollutant", out var pollutant)
                   && string.Equals(pollutant, "NO2", StringComparison.OrdinalIgnoreCase)
                   && TryGetFloat(feature, "concentration_no2_ug_m3", out _);
        }

        private bool MatchesKind(Feature feature)
        {
            return DetectKind(feature) == Kind;
        }

        private static bool TryGetPropertyByPrefix(Feature feature, string prefix, out object value)
        {
            value = null;
            if (feature?.Properties == null)
                return false;

            foreach (var property in feature.Properties)
            {
                if (!property.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                value = property.Value;
                return true;
            }
            return false;
        }

        private static bool TryGetString(Feature feature, string key, out string value)
        {
            value = null;
            if (feature?.Properties == null
                || !feature.Properties.TryGetValue(key, out var rawValue)
                || rawValue == null)
                return false;

            value = rawValue.ToString();
            return !string.IsNullOrWhiteSpace(value);
        }

        private static bool TryGetFloat(Feature feature, string key, out float value)
        {
            value = 0f;
            return TryGetString(feature, key, out var text)
                   && (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                       || float.TryParse(text, out value));
        }

        private sealed class Metric
        {
            public string Name { get; }
            public string PropertyKey { get; }
            public float Minimum { get; }
            public float Maximum { get; }
            public bool PositiveIsGood { get; }
            public bool IsWeightedScore { get; }
            public bool UsesSafetyPalette => IsWeightedScore;

            public Metric(
                string name,
                string propertyKey,
                float minimum,
                float maximum,
                bool positiveIsGood = false,
                bool isWeightedScore = false)
            {
                Name = name;
                PropertyKey = propertyKey;
                Minimum = minimum;
                Maximum = maximum;
                PositiveIsGood = positiveIsGood;
                IsWeightedScore = isWeightedScore;
            }
        }

        private sealed class FeatureReferenceComparer : IEqualityComparer<Feature>
        {
            public bool Equals(Feature left, Feature right)
            {
                return ReferenceEquals(left, right);
            }

            public int GetHashCode(Feature feature)
            {
                return RuntimeHelpers.GetHashCode(feature);
            }
        }
    }
}
