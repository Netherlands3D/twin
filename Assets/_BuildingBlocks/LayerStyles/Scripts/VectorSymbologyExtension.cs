using UnityEngine;

namespace Netherlands3D.LayerStyles
{
    /// <link href="https://docs.ogc.org/DRAFTS/18-067r4.html#rc-vector" />
    public static class VectorSymbologyExtension
    {
        /// <link href="https://docs.ogc.org/DRAFTS/18-067r4.html#_fills"/>
        public static void SetFillColor(this Symbolizer symbology, Color color)
        {
            symbology.SetProperty("fill-color", ColorUtility.ToHtmlStringRGBA(color));
        }

        /// <link href="https://docs.ogc.org/DRAFTS/18-067r4.html#_fills"/>
        public static Color? GetFillColor(this Symbolizer symbology)
        {
            var property = symbology.GetProperty("fill-color") as string;

            if (!ColorUtility.TryParseHtmlString(property, out var color)) return null;

            return color;
        }

        /// <link href="https://docs.ogc.org/DRAFTS/18-067r4.html#_strokes"/>
        public static void SetStrokeColor(this Symbolizer symbology, Color color)
        {
            symbology.SetProperty("stroke-color", ColorUtility.ToHtmlStringRGBA(color));
        }

        /// <link href="https://docs.ogc.org/DRAFTS/18-067r4.html#_strokes"/>
        public static Color? GetStrokeColor(this Symbolizer symbology)
        {
            var property = symbology.GetProperty("stroke-color") as string;

            if (!ColorUtility.TryParseHtmlString(property, out var color)) return null;

            return color;
        }
    }
}