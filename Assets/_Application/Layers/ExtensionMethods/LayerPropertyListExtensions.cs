using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers.Properties;

namespace Netherlands3D.Twin.Layers.ExtensionMethods
{
    public static class LayerPropertyListExtensions
    {
        public static T Get<T>(this List<LayerPropertyData> properties) where T : LayerPropertyData
        {
            return properties.OfType<T>().FirstOrDefault();
        }

        public static LayerPropertyData Get(this List<LayerPropertyData> properties, Type type) 
        {
            return properties.FirstOrDefault(data => data.GetType() == type);
        }
    }
}