using System.Collections.Generic;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers
{
    public interface IFeatureVisualisation<T>
    {
        public List<T> Data { get; }
        public void CalculateBounds();
    }
}