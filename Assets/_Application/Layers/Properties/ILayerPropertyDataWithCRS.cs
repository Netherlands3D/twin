using System.Collections.Generic;

namespace Netherlands3D.Twin.Layers.Properties
{
    public interface ILayerPropertyDataWithCRS
    {
        /// <summary>
        /// Returns any layer asset associated with this property; the individual properties
        /// contain the URI referencing the assets.
        /// </summary>
        /// <returns>Series of asset objects</returns>
        public int ContentCRS { get; set; }
    }
}