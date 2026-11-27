using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.DataSets
{
    /// <summary>
    /// Resolves the shared dashboard palette and stable colors for additional CSV segment names.
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

        public static string SegmentKey(string segment) => segment switch
        {
            "Sociaal" => "social",
            "Middenhuur" => "mid-rent",
            "Vrije sector" => "free-sector",
            _ => "segment:" + segment.ToUpperInvariant()
        };

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
