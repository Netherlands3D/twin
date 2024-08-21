using System.Collections.Generic;

namespace Netherlands3D.Twin.Layers.Properties
{
    public interface ILayerPropertyDataWithAssets
    {
        public IEnumerable<Asset> GetAssets();
    }
}