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
        public static void SetMaskLayerMask(LayerGameObject layer, int rBitMask) //todo: g and b
        {
            layer.LayerData.DefaultStyle.AnyFeature.Symbolizer.SetMaskLayerMask(rBitMask, 0, 0);
            layer.ApplyStyling();
        }

        /// <summary>
        /// Retrieves the bitmask for masking of the LayerGameObject.
        /// </summary>
        public static int[] GetMaskLayerMask(LayerGameObject layer)
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
    }
}