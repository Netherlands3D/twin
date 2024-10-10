using UnityEngine;

namespace Netherlands3D.LayerStyles
{
    public static class VectorSymbologyExtension
    {
        public static void SetFillColor(this Symbolizer symbology, Color color)
        {
            symbology.SetProperty("fillColor", $"#{ColorUtility.ToHtmlStringRGB(color)}");
        }

        public static Color GetFillColor(this Symbolizer symbology)
        {
            var colorString = symbology.GetProperty("fill") as string ?? "";
            if (string.IsNullOrEmpty(colorString)) return default;

            var success = ColorUtility.TryParseHtmlString(colorString, out var color);
            if (!success) return default;

            return color;
        }
    }
}