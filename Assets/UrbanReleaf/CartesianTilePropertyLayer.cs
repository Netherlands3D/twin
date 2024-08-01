using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public class CartesianTilePropertyLayer : CartesianTileLayer, ILayerWithProperties
    {
        [SerializeField] private bool usePropertySections = true;
        [SerializeField] private bool openPropertiesOnStart = true;
        private List<IPropertySectionInstantiator> propertySections = new();

        public List<IPropertySectionInstantiator> GetPropertySections()
        {
            return propertySections;
        }

        protected override void Awake()
        {
            base.Awake();
            if (usePropertySections)
                propertySections = GetComponents<IPropertySectionInstantiator>().ToList();
            else
                propertySections = new();
        }
    }
}