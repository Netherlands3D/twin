using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;

namespace Netherlands3D.Functionalities.UrbanReLeaf
{
    public class CartesianTilePropertyLayer : CartesianTileLayerGameObject, ILayerWithPropertyPanels
    {
        private List<IPropertySectionInstantiator> propertySections = new();

        public List<IPropertySectionInstantiator> GetPropertySections()
        {
            propertySections = GetComponents<IPropertySectionInstantiator>().ToList();
            return propertySections;
        }
    }
}