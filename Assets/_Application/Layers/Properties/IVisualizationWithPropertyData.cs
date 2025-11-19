using System.Collections.Generic;

namespace Netherlands3D.Twin.Layers.Properties
{
    public interface IVisualizationWithPropertyData
    {
        public void LoadProperties(List<LayerPropertyData> properties);
    }
}