using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public class CartesianTilePropertyLayer : CartesianTileLayerGameObject, ILayerWithProperties
    {
        private List<IPropertySectionInstantiator> propertySections = new();

        public List<IPropertySectionInstantiator> GetPropertySections()
        {
            propertySections = GetComponents<IPropertySectionInstantiator>().ToList();
            return propertySections;
        }
    }
}