using UnityEngine;

namespace Netherlands3D.LayerStyles
{
    public static class VectorSymbologyExtension
    {
        public static void SetFillColor(this Symbolizer symbology, Color color)
        {
            symbology.SetProperty("fill", color);
        }

        public static Color? GetFillColor(this Symbolizer symbology)
        {
            return symbology.GetProperty("fill") as Color?;
        }
    }
}