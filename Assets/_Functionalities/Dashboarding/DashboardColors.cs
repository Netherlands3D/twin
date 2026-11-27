using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.DataSets
{
    /// <summary>
    /// Resolves the shared dashboard palette and category scales for the current CSV data.
    /// </summary>
    internal static class DashboardColors
    {
        private static readonly (string Key, CustomStyleProperty<Color> Property)[] Properties =
        {
            ("base", new("--color-dashboard-map-base")),
            ("social", new("--color-dashboard-segment-social")), ("mid-rent", new("--color-dashboard-segment-mid-rent")),
            ("free-sector", new("--color-dashboard-segment-free-sector")),
            ("low", new("--color-dashboard-dwellings-low")), ("medium", new("--color-dashboard-dwellings-medium")),
            ("high", new("--color-dashboard-dwellings-high")),
            ("bbp", new("--color-dashboard-phase-bbp")), ("so", new("--color-dashboard-phase-so")),
            ("vo", new("--color-dashboard-phase-vo")), ("do", new("--color-dashboard-phase-do")),
            ("phase-unavailable", new("--color-dashboard-phase-unavailable")),
            ("Bedrijf", new("--color-dashboard-category-business")), ("Kantoor", new("--color-dashboard-category-office")),
            ("Horeca", new("--color-dashboard-category-hospitality")), ("Maatschappelijk", new("--color-dashboard-category-social")),
            ("Detailhandel", new("--color-dashboard-category-retail")), ("Supermarkt", new("--color-dashboard-category-supermarket")),
            ("Sport", new("--color-dashboard-category-sport")),
            ("Bestaand programma - kantoor", new("--color-dashboard-category-existing-office")),
            ("Kantoor thuiswerk", new("--color-dashboard-category-home-office")),
            ("1 kamer", new("--color-dashboard-room-one")), ("2 kamers", new("--color-dashboard-room-two")),
            ("3 kamers", new("--color-dashboard-room-three")), ("4 kamers", new("--color-dashboard-room-four"))
        };

        public static bool Read(ICustomStyle styles, IDictionary<string, Color> colors)
        {
            var changed = false;
            foreach (var (key, property) in Properties)
            {
                if (!styles.TryGetValue(property, out var color) || colors.TryGetValue(key, out var current) && current == color)
                    continue;

                colors[key] = color;
                changed = true;
            }
            return changed;
        }

        public static bool HasMapColors(IReadOnlyDictionary<string, Color> colors)
        {
            foreach (var key in new[] { "base", "social", "mid-rent", "free-sector", "low", "medium", "high", "bbp", "so", "vo", "do" })
            {
                if (!colors.ContainsKey(key))
                    return false;
            }
            return true;
        }

        public static string SegmentKey(string segment) => "segment:" + segment.ToUpperInvariant();

        public static string PhaseKey(string phase) => "phase:" + phase.ToUpperInvariant();

        public static void SetSegmentColors(IEnumerable<string> segments, IDictionary<string, Color> colors) =>
            SetScaleColors(segments, "segment:", new[] { "social", "mid-rent", "free-sector" }, colors);

        public static void SetRoomColors(IEnumerable<string> rooms, IDictionary<string, Color> colors) =>
            SetScaleColors(rooms, "room:", new[] { "1 kamer", "2 kamers", "3 kamers", "4 kamers" }, colors);

        public static void SetPhaseColors(IEnumerable<string> phases, IDictionary<string, Color> colors)
        {
            SetScaleColors(phases, "phase:", new[] { "bbp", "so", "vo", "do" }, colors);
            if (!colors.TryGetValue("phase-unavailable", out var unavailable))
                return;

            foreach (var phase in DashboardData.Phases)
                colors.TryAdd(PhaseKey(phase), unavailable);
        }

        private static void SetScaleColors(IEnumerable<string> labels, string prefix, string[] stops, IDictionary<string, Color> colors)
        {
            foreach (var key in colors.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray())
                colors.Remove(key);

            if (stops.Any(key => !colors.ContainsKey(key)))
                return;

            var entries = labels.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            for (var i = 0; i < entries.Length; i++)
            {
                var position = entries.Length > 1 ? (float)i / (entries.Length - 1) * (stops.Length - 1) : 0;
                var lower = Mathf.Min(Mathf.FloorToInt(position), stops.Length - 2);
                colors[prefix + entries[i].ToUpperInvariant()] = Color.Lerp(colors[stops[lower]], colors[stops[lower + 1]], position - lower);
            }
        }

        public static Color GetSegmentColor(string segment, IReadOnlyDictionary<string, Color> colors) =>
            colors.TryGetValue(SegmentKey(segment), out var color) ? color : GetColor(segment, colors);

        public static Color GetRoomColor(string room, IReadOnlyDictionary<string, Color> colors) =>
            colors.TryGetValue("room:" + room.ToUpperInvariant(), out var color) ? color : GetColor(room, colors);

        public static Color GetPhaseColor(string phase, IReadOnlyDictionary<string, Color> colors) =>
            colors.TryGetValue(PhaseKey(phase), out var color) ? color : GetColor(phase, colors);

        public static Color GetColor(string label, IReadOnlyDictionary<string, Color> colors)
        {
            var key = label switch { "Sociaal" => "social", "Middenhuur" => "mid-rent", "Vrije sector" => "free-sector", _ => label };
            if (colors.TryGetValue(key, out var color))
                return color;
            if (key == "social" || key == "mid-rent" || key == "free-sector")
                return Color.gray;

            uint hash = 2166136261;
            foreach (var character in label.ToUpperInvariant())
                hash = unchecked((hash ^ character) * 16777619);
            return Color.HSVToRGB((hash % 360) / 360f, 0.65f, 0.7f);
        }
    }
}
