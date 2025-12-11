using Netherlands3D.Twin.Layers.Properties;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class MaskingLayerPropertyData : StylingPropertyData
    {
        public const int DEFAULT_MASK_BIT_MASK = 16777215; //(2^24)-1; 
        
        /// <summary>
        /// Sets a bitmask to the layer to determine which masks affect the provided LayerGameObject
        /// </summary>
        public void SetMaskBitMask(int bitMask)
        {
            AnyFeature.Symbolizer.SetMaskLayerMask(bitMask);
            OnStylingChanged.Invoke();
        }
        
        public void SetMaskBit(int bitIndex, bool enableBit)
        {
            var currentLayerMask = GetMaskLayerMask();
            int maskBitToSet = 1 << bitIndex;

            if (enableBit)
            {
                currentLayerMask |= maskBitToSet; // set bit to 1
            }
            else
            {
                currentLayerMask &= ~maskBitToSet; // set bit to 0
            }

            SetMaskBitMask(currentLayerMask);
        }

        /// <summary>
        /// Retrieves the bitmask for masking of the LayerGameObject.
        /// </summary>
        public int GetMaskLayerMask()
        {
            // if (stylingPropertyData == null) return DEFAULT_MASK_BIT_MASK;

            int? bitMask = AnyFeature.Symbolizer.GetMaskLayerMask();
            if (bitMask == null)
                bitMask = DEFAULT_MASK_BIT_MASK;

            return bitMask.Value;
        }
    }
}
