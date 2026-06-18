using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    /// <summary>
    /// A NumberField that displays and parses its value as "hh:mm" instead of a plain
    /// decimal number. The underlying double value represents total minutes, so it
    /// naturally supports negative durations and hour counts beyond 24
    /// (e.g. "36:15" for a 36h15m duration).
    /// </summary>
    // [UxmlElement]
    // public partial class TimeField : VisualElement
    // {
    //     private const char timeSeparator = ':';
    //
    //     public TimeField() : base()
    //     {
    //     }
    //     
    //     protected override string FormatValue(double totalMinutes)
    //     {
    //         var isNegative = totalMinutes < 0;
    //         var absMinutes = Math.Abs(totalMinutes);
    //
    //         var hours = (int)(absMinutes / 60);
    //         var minutes = (int)Math.Round(absMinutes % 60, MidpointRounding.AwayFromZero);
    //
    //         // Rounding can push minutes to exactly 60 (e.g. 89.6 minutes -> 1h, 60m)
    //         if (minutes >= 60)
    //         {
    //             minutes -= 60;
    //             hours += 1;
    //         }
    //
    //         var sign = isNegative ? "-" : string.Empty;
    //         Debug.Log(sign + hours + ":" + minutes);
    //         return $"{sign}{hours:00}{timeSeparator}{minutes:00}{UnitCharacter}";
    //     }
    //
    //     protected override double ParseValue(string text)
    //     {
    //         Debug.Log($"TimeField.ParseValue('{text}')");
    //         if (string.IsNullOrWhiteSpace(text))
    //             return 0d;
    //
    //         var trimmed = text.Trim();
    //         if (UnitCharacter.Length > 0)
    //             trimmed = trimmed.Replace(UnitCharacter, string.Empty).Trim();
    //
    //         var isNegative = trimmed.StartsWith("-");
    //         if (isNegative)
    //             trimmed = trimmed.Substring(1);
    //
    //         var parts = trimmed.Split(timeSeparator);
    //
    //         if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var hours))
    //             return 0d;
    //
    //         var minutes = 0;
    //         if (parts.Length > 1)
    //             int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out minutes);
    //
    //         var totalMinutes = (double)(hours * 60 + minutes);
    //         return isNegative ? -totalMinutes : totalMinutes;
    //     }
    //
    //     /// <summary>
    //     /// Sets the field's value from the time-of-day portion of a DateTime (hours and
    //     /// minutes; seconds are ignored).
    //     /// </summary>
    //     public void SetValueWithoutNotify(DateTime dateTime)
    //     {
    //         var totalMinutes = dateTime.Hour * 60 + dateTime.Minute;
    //         SetValueWithoutNotify((double)totalMinutes);
    //     }
    //
    //     /// <summary>
    //     /// Returns the field's value as a DateTime, using today's date as the base with
    //     /// the parsed total minutes applied as a time-of-day offset. If the value
    //     /// represents a duration beyond 24h (or a negative duration), the date portion
    //     /// rolls forward/backward accordingly.
    //     /// </summary>
    //     public (int hour, int minute) GetValueAsTime()
    //     {
    //         var minutes = GetValueAsDouble();
    //         Debug.Log("DoubleValue: " + minutes);
    //         return ((int)minutes/60, (int)minutes%60 );
    //     }
    // }
}