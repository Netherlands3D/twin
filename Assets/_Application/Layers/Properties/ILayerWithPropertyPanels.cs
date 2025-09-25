using System.Collections.Generic;
using Netherlands3D.Twin.Layers.Properties;

namespace Netherlands3D.Twin.Layers.LayerTypes
{
    public interface ILayerWithPropertyPanels
    {
        public bool HasPropertySections
        {
            get
            {
                var propertySections = GetPropertySections();
                if (propertySections == null) return false;
                
                return propertySections.Count > 0;
            }
        }

        public List<IPropertySectionInstantiator> GetPropertySections();
    }
}