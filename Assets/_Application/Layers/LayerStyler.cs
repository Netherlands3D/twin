using System;
using Netherlands3D.LayerStyles;

namespace Netherlands3D.Twin.Layers
{
    /// <summary>
    /// Helper class that helps with styling LayerGameObjects.
    /// </summary>
    public static class LayerStyler
    {
        /// <summary>
        /// Sets a bitmask to the layer to determine which masks affect the provided LayerGameObject
        /// </summary>
        public static void SetMaskLayerMask(LayerGameObject layer, int bitMask)
        {
            layer.LayerData.DefaultStyle.AnyFeature.Symbolizer.SetMaskLayerMask(bitMask);
            layer.ApplyStyling();
        }

        /// <summary>
        /// Retrieves the bitmask for masking of the LayerGameObject.
        /// </summary>
        public static int GetMaskLayerMask(LayerGameObject layer)
        {
            return layer.LayerData.DefaultStyle.AnyFeature.Symbolizer.GetMaskLayerMask();
        }

        /// <summary>
        /// Applies the Styling to the LayerGameObject
        /// </summary>
        public static void Apply(LayerGameObject layer, Symbolizer styling)
        {
            layer.UpdateMaskBitMask(styling.GetMaskLayerMask());
        }
        
        public static int StringToBitmask(string bitString)
        {
            if (string.IsNullOrEmpty(bitString))
                throw new ArgumentException("Input string cannot be null or empty.", nameof(bitString));

            return Convert.ToInt32(bitString, 2);
        }
    }
}