using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI_Toolkit.Scripts
{
    internal static class ChartUtility
    {
        public static string FormatValue(decimal value, CultureInfo culture, string format = "#,0.##", bool compact = false, string thousandSuffix = "k", string millionSuffix = "M", string billionSuffix = "B")
        {
            var magnitude = Math.Abs(value);
            if (!compact || magnitude < 9_999m)
                return value.ToString(format, culture);

            var divisor = magnitude >= 1_000_000_000m ? 1_000_000_000m : magnitude >= 1_000_000m ? 1_000_000m : 1_000m;
            var rounded = decimal.Round(value / divisor, 1, MidpointRounding.AwayFromZero);
            if (Math.Abs(rounded) >= 1_000m && divisor < 1_000_000_000m)
            {
                divisor *= 1_000m;
                rounded = decimal.Round(value / divisor, 1, MidpointRounding.AwayFromZero);
            }

            var suffix = divisor == 1_000m ? thousandSuffix : divisor == 1_000_000m ? millionSuffix : billionSuffix;
            return rounded.ToString("0.#", culture) + suffix;
        }

        public static void AddLegendEntry(VisualElement legend, string stylePrefix, string label, Color color, string tooltip = null)
        {
            var entry = new VisualElement { tooltip = tooltip };
            entry.AddToClassList($"{stylePrefix}__legend-entry");
            entry.AddToClassList("legend-entry");
            var swatch = new VisualElement { pickingMode = PickingMode.Ignore };
            swatch.AddToClassList($"{stylePrefix}__swatch");
            swatch.AddToClassList("legend-swatch");
            swatch.style.backgroundColor = color;
            entry.Add(swatch);
            entry.Add(new Label(label));
            legend.Add(entry);
        }
    }
}
