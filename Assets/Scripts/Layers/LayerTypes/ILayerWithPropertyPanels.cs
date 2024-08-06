using System.Collections.Generic;
using Netherlands3D.Twin.Layers.Properties;

namespace Netherlands3D.Twin.Layers.LayerTypes
{
    public interface ILayerWithPropertyPanels
    {
        public List<IPropertySectionInstantiator> GetPropertySections();
    }
}