using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using Netherlands3D.LayerStyles;
using Netherlands3D.Services;
using Netherlands3D.Sun;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D
{
    /// <summary>
    /// Adds timeline behaviour to GeoJSON layers. Traffic time-series data is indexed and rendered as a filtered
    /// slice so thousands of repeated geometries do not overlap. Other timestamp/count GeoJSON files retain the
    /// original layer-wide color behaviour.
    /// </summary>
    [RequireComponent(typeof(LayerGameObject))]
    public class TimelineGeoJson : MonoBehaviour
    {
        private static readonly string[] VehicleOrder =
        {
            "car", "bicycle", "moped", "motorcycle", "medium_heavy", "heavy"
        };

        private static readonly string[] DayTypeOrder =
        {
            "workday", "weekday", "saturday", "sunday"
        };

        private static readonly float[] ColorStops = { 0f, 0.25f, 0.5f, 0.75f, 1f };
        private static readonly Color[] TrafficColors =
        {
            new(0.173f, 0.482f, 0.714f),
            new(0.255f, 0.714f, 0.655f),
            new(1.000f, 0.878f, 0.545f),
            new(0.957f, 0.427f, 0.263f),
            new(0.647f, 0.000f, 0.149f)
        };

        public static TimelineGeoJson ActiveTimeline { get; private set; }
        public static event Action<TimelineGeoJson> ActiveTimelineChanged;

        public event Action<TimelineGeoJson> TimelineStateChanged;
        public event Action<TimelineGeoJson> TimelineTimeChanged;

        public bool HasTimelineData { get; private set; }
        public bool HasTrafficData { get; private set; }
        public bool ParsingComplete { get; private set; }
        public IReadOnlyList<string> AvailableVehicleTypes => availableVehicleTypes;
        public IReadOnlyList<string> AvailableDayTypes => availableDayTypes;
        public string SelectedVehicleType { get; private set; }
        public string SelectedDayType { get; private set; }
        public DateTime CurrentTime => currentTime;
        public int CurrentHour => currentTime.Hour;
        public int RenderedTrafficHour { get; private set; } = -1;
        public int VisibleRouteCount { get; private set; }
        public string LayerName => visualization?.LayerData?.Name ?? "Verkeersdata";

        private readonly List<Feature> trafficFeatures = new();
        private readonly HashSet<Feature> indexedTrafficFeatures = new(new FeatureReferenceComparer());
        private readonly HashSet<string> vehicleTypes = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> dayTypes = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<float>> countsByVehicle = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, float> scaleMaximumByVehicle = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> availableVehicleTypes = new();
        private readonly List<string> availableDayTypes = new();

        private GeoJsonLayerGameObject visualization;
        private ColorPropertyData stylingPropertyData;
        private SunTime sunTime;
        private DateTime currentTime = DateTime.Today;
        private Func<Feature, bool> previousVisualisationFilter;
        private Func<Feature, bool> combinedVisualisationFilter;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            ActiveTimeline = null;
            ActiveTimelineChanged = null;
        }

        private void Awake()
        {
            visualization = GetComponent<GeoJsonLayerGameObject>();
            if (visualization == null)
            {
                // Some legacy line-layer prefabs also contain this behaviour but use GeoJSONLineLayer directly.
                // Leave those existing prefabs untouched; timeline filtering is supported by GeoJsonLayerGameObject.
                enabled = false;
                return;
            }

            previousVisualisationFilter = visualization.FeatureVisualisationFilter;
            combinedVisualisationFilter = feature =>
                (previousVisualisationFilter?.Invoke(feature) ?? true) && !IsTrafficFeature(feature);
            visualization.FeatureVisualisationFilter = combinedVisualisationFilter;

            visualization.FeatureAdded += OnFeatureAdded;
            visualization.Parser.OnParseCompleted.AddListener(OnParseCompleted);
        }

        private void Start()
        {
            if (visualization == null)
                return;

            visualization.InitProperty<TimelineLayerPropertyData>(visualization.LayerData.LayerProperties);
            stylingPropertyData = visualization.LayerData.LayerProperties
                .GetDefaultStylingPropertyData<ColorPropertyData>();

            visualization.LayerData.LayerSelected.AddListener(OnLayerSelected);
            visualization.LayerData.LayerDeselected.AddListener(OnLayerDeselected);

            sunTime = ServiceLocator.GetService<SunTime>();
            if (sunTime != null)
            {
                currentTime = sunTime.Time;
                sunTime.timeOfDayChanged.AddListener(OnTimeChanged);
            }

            // Covers unusual initialization orders where parsing completed before Start.
            foreach (var feature in visualization.GeoJsonFeatures)
                IndexFeature(feature);

            if (visualization.Parser.HasCompleted)
                FinalizeTrafficIndex();

            if (visualization.LayerData.IsSelected && HasTimelineData)
                SetActiveTimeline(this);
        }

        private void OnDestroy()
        {
            if (visualization == null)
                return;

            visualization.FeatureAdded -= OnFeatureAdded;
            visualization.Parser.OnParseCompleted.RemoveListener(OnParseCompleted);

            if (visualization.FeatureVisualisationFilter == combinedVisualisationFilter)
                visualization.FeatureVisualisationFilter = previousVisualisationFilter;

            if (visualization.HasLayerData)
            {
                visualization.LayerData.LayerSelected.RemoveListener(OnLayerSelected);
                visualization.LayerData.LayerDeselected.RemoveListener(OnLayerDeselected);
            }

            sunTime?.timeOfDayChanged.RemoveListener(OnTimeChanged);

            if (ActiveTimeline == this)
                SetActiveTimeline(null);
        }

        public void SelectVehicleType(string vehicleType)
        {
            if (!availableVehicleTypes.Contains(vehicleType, StringComparer.OrdinalIgnoreCase))
                return;

            SelectedVehicleType = vehicleType;
            ApplyTrafficSlice();
        }

        public void SelectDayType(string dayType)
        {
            if (!availableDayTypes.Contains(dayType, StringComparer.OrdinalIgnoreCase))
                return;

            SelectedDayType = dayType;
            ApplyTrafficSlice();
        }

        public float GetScaleMaximumForSelectedVehicle()
        {
            return SelectedVehicleType != null
                   && scaleMaximumByVehicle.TryGetValue(SelectedVehicleType, out var maximum)
                ? maximum
                : 0f;
        }

        public float GetCountAtNormalizedIntensity(float intensity)
        {
            var maximum = GetScaleMaximumForSelectedVehicle();
            if (maximum <= 0f)
                return 0f;

            return Mathf.Exp(Mathf.Log(1f + maximum) * Mathf.Clamp01(intensity)) - 1f;
        }

        public static Color GetTrafficColor(float normalizedIntensity)
        {
            var value = Mathf.Clamp01(normalizedIntensity);
            for (var i = 1; i < ColorStops.Length; i++)
            {
                if (value > ColorStops[i])
                    continue;

                var t = Mathf.InverseLerp(ColorStops[i - 1], ColorStops[i], value);
                return Color.Lerp(TrafficColors[i - 1], TrafficColors[i], t);
            }

            return TrafficColors[^1];
        }

        public static string GetVehicleDisplayName(string vehicleType)
        {
            return vehicleType switch
            {
                "car" => "Auto",
                "bicycle" => "Fiets",
                "moped" => "Bromfiets",
                "motorcycle" => "Motor",
                "medium_heavy" => "Middelzwaar",
                "heavy" => "Zwaar",
                _ => vehicleType
            };
        }

        public static string GetDayTypeDisplayName(string dayType)
        {
            return dayType switch
            {
                "workday" => "Werkdag",
                "weekday" => "Weekdag",
                "saturday" => "Zaterdag",
                "sunday" => "Zondag",
                _ => dayType
            };
        }

        private void OnFeatureAdded(Feature feature)
        {
            var previouslyHadTimelineData = HasTimelineData;
            var previouslyHadTrafficData = HasTrafficData;
            IndexFeature(feature);

            if (visualization.HasLayerData && visualization.LayerData.IsSelected && HasTimelineData)
                SetActiveTimeline(this);

            if (previouslyHadTimelineData != HasTimelineData || previouslyHadTrafficData != HasTrafficData)
                TimelineStateChanged?.Invoke(this);
        }

        private void IndexFeature(Feature feature)
        {
            if (feature?.Properties == null)
                return;

            var isTrafficFeature = IsTrafficFeature(feature);
            if (isTrafficFeature
                || TryGetFloat(feature, "count", out _) && TryGetDateTime(feature, "timestamp", out _))
                HasTimelineData = true;

            if (!isTrafficFeature || !indexedTrafficFeatures.Add(feature))
                return;

            HasTrafficData = true;
            HasTimelineData = true;
            trafficFeatures.Add(feature);

            TryGetString(feature, "vehicle_class", out var vehicleType);
            TryGetString(feature, "day_type", out var dayType);
            TryGetFloat(feature, "count", out var count);

            vehicleTypes.Add(vehicleType);
            dayTypes.Add(dayType);
            if (!countsByVehicle.TryGetValue(vehicleType, out var counts))
            {
                counts = new List<float>();
                countsByVehicle.Add(vehicleType, counts);
            }
            counts.Add(Mathf.Max(0f, count));
        }

        private void OnParseCompleted()
        {
            FinalizeTrafficIndex();
        }

        private void FinalizeTrafficIndex()
        {
            ParsingComplete = true;

            availableVehicleTypes.Clear();
            availableVehicleTypes.AddRange(OrderKnownValues(vehicleTypes, VehicleOrder));
            availableDayTypes.Clear();
            availableDayTypes.AddRange(OrderKnownValues(dayTypes, DayTypeOrder));

            scaleMaximumByVehicle.Clear();
            foreach (var pair in countsByVehicle)
            {
                pair.Value.Sort();
                var percentileIndex = Mathf.Clamp(
                    Mathf.CeilToInt(pair.Value.Count * 0.95f) - 1,
                    0,
                    pair.Value.Count - 1);
                scaleMaximumByVehicle[pair.Key] = pair.Value.Count == 0 ? 0f : pair.Value[percentileIndex];
            }

            if (SelectedVehicleType == null || !availableVehicleTypes.Contains(SelectedVehicleType))
                SelectedVehicleType = availableVehicleTypes.Contains("car")
                    ? "car"
                    : availableVehicleTypes.FirstOrDefault();

            if (SelectedDayType == null || !availableDayTypes.Contains(SelectedDayType))
                SelectedDayType = availableDayTypes.Contains("workday")
                    ? "workday"
                    : availableDayTypes.FirstOrDefault();

            if (HasTrafficData)
                ApplyTrafficSlice();
            else
                TimelineStateChanged?.Invoke(this);
        }

        private static IEnumerable<string> OrderKnownValues(IEnumerable<string> values, IReadOnlyList<string> preferredOrder)
        {
            return values.OrderBy(value =>
            {
                for (var index = 0; index < preferredOrder.Count; index++)
                {
                    if (string.Equals(value, preferredOrder[index], StringComparison.OrdinalIgnoreCase))
                        return index;
                }
                return preferredOrder.Count;
            }).ThenBy(value => value, StringComparer.OrdinalIgnoreCase);
        }

        private void OnLayerSelected(LayerData _)
        {
            if (HasTimelineData)
                SetActiveTimeline(this);
        }

        private void OnLayerDeselected(LayerData _)
        {
            if (ActiveTimeline == this)
                SetActiveTimeline(null);
        }

        private static void SetActiveTimeline(TimelineGeoJson timeline)
        {
            if (ActiveTimeline == timeline)
                return;

            ActiveTimeline = timeline;
            ActiveTimelineChanged?.Invoke(timeline);
        }

        private void OnTimeChanged(DateTime selectedTime)
        {
            currentTime = selectedTime;

            if (HasTrafficData)
            {
                TimelineTimeChanged?.Invoke(this);

                // Compare against the slice that is actually on screen. Comparing consecutive clock values can
                // miss a refresh when another listener or an initialization step has already advanced the clock.
                if (ParsingComplete && RenderedTrafficHour != currentTime.Hour)
                    ApplyTrafficSlice();
                return;
            }

            ApplyLegacyTimelineColor(selectedTime);
            TimelineStateChanged?.Invoke(this);
        }

        private void ApplyTrafficSlice()
        {
            if (!ParsingComplete || string.IsNullOrEmpty(SelectedVehicleType) || string.IsNullOrEmpty(SelectedDayType))
            {
                TimelineStateChanged?.Invoke(this);
                return;
            }

            var visibleFeatures = new List<Feature>();
            var featureColors = new Dictionary<Feature, Color>();
            var featureWidths = new Dictionary<Feature, float>();

            foreach (var feature in trafficFeatures)
            {
                if (!TryGetString(feature, "vehicle_class", out var vehicleType)
                    || !string.Equals(vehicleType, SelectedVehicleType, StringComparison.OrdinalIgnoreCase)
                    || !TryGetString(feature, "day_type", out var dayType)
                    || !string.Equals(dayType, SelectedDayType, StringComparison.OrdinalIgnoreCase)
                    || !TryGetHour(feature, out var hour)
                    || hour != currentTime.Hour
                    || !TryGetFloat(feature, "count", out var count))
                    continue;

                var intensity = NormalizeCount(vehicleType, count);
                visibleFeatures.Add(feature);
                featureColors[feature] = GetTrafficColor(intensity);
                featureWidths[feature] = Mathf.Lerp(0.85f, 2f, Mathf.Pow(intensity, 0.7f));
            }

            VisibleRouteCount = visibleFeatures.Count;
            visualization.SetVisibleTimelineLineFeatures(visibleFeatures, featureColors, featureWidths);
            RenderedTrafficHour = currentTime.Hour;
            TimelineStateChanged?.Invoke(this);
        }

        private float NormalizeCount(string vehicleType, float count)
        {
            if (!scaleMaximumByVehicle.TryGetValue(vehicleType, out var maximum) || maximum <= 0f)
                return count > 0f ? 1f : 0f;

            return Mathf.Clamp01(Mathf.Log(1f + Mathf.Max(0f, count)) / Mathf.Log(1f + maximum));
        }

        private void ApplyLegacyTimelineColor(DateTime selectedTime)
        {
            stylingPropertyData = visualization.LayerData.LayerProperties
                .GetDefaultStylingPropertyData<ColorPropertyData>();
            if (stylingPropertyData == null || !TryGetLegacyColor(selectedTime, out var color))
                return;

            stylingPropertyData.ColorType = Symbolizer.StrokeColorProperty;
            stylingPropertyData.SetDefaultSymbolizerColor(color);
        }

        private bool TryGetLegacyColor(DateTime selectedTime, out Color color)
        {
            color = default;
            foreach (var feature in visualization.GeoJsonFeatures)
            {
                if (IsTrafficFeature(feature)
                    || !TryGetDateTime(feature, "timestamp", out var timestamp)
                    || !TryGetFloat(feature, "count", out var count)
                    || selectedTime < timestamp
                    || selectedTime >= timestamp.AddHours(1d))
                    continue;

                color = count switch
                {
                    < 10f => Color.green,
                    < 20f => Color.yellow,
                    < 30f => new Color(1f, 0.5f, 0f),
                    _ => Color.red
                };
                return true;
            }

            return false;
        }

        private static bool IsTrafficFeature(Feature feature)
        {
            return feature?.Geometry != null
                   && (feature.Geometry.Type == GeoJSONObjectType.LineString
                       || feature.Geometry.Type == GeoJSONObjectType.MultiLineString)
                   && TryGetString(feature, "vehicle_class", out _)
                   && TryGetString(feature, "day_type", out _)
                   && TryGetFloat(feature, "count", out _)
                   && TryGetHour(feature, out _);
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
            if (!TryGetString(feature, key, out var text))
                return false;

            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                   || float.TryParse(text, out value);
        }

        private static bool TryGetHour(Feature feature, out int hour)
        {
            hour = 0;
            if (TryGetFloat(feature, "hour", out var numericHour))
            {
                hour = Mathf.Clamp(Mathf.RoundToInt(numericHour), 0, 23);
                return true;
            }

            if (!TryGetDateTime(feature, "timestamp", out var timestamp))
                return false;

            hour = timestamp.Hour;
            return true;
        }

        private static bool TryGetDateTime(Feature feature, string key, out DateTime value)
        {
            value = default;
            return TryGetString(feature, key, out var text)
                   && DateTime.TryParse(
                       text,
                       CultureInfo.InvariantCulture,
                       DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind,
                       out value);
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
