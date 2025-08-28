using System.Collections.Generic;
using Netherlands3D.Twin.Layers.Properties;

namespace Netherlands3D.Twin.Layers.LayerTypes
{
    public interface ILayerWithPropertyPanels
    {
        public bool HasPropertySections => (GetPropertySections()?.Count ?? 0) > 0;
        public List<IPropertySectionInstantiator> GetPropertySections();
    }
}