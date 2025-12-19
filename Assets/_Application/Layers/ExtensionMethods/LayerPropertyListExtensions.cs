using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers.Properties;

namespace Netherlands3D.Twin.Layers.ExtensionMethods
{
    public static class LayerPropertyListExtensions
    {
        public static bool Set<T>(this List<LayerPropertyData> properties, T propertyData) where T : LayerPropertyData
        {
            var existingProperty = properties.Get<T>();
            if (existingProperty == null)
            {
                properties.Add(propertyData);
                return true;
            }

            if (existingProperty == propertyData)
            {
                return false;
            }

            int index = properties.IndexOf(existingProperty);
            properties[index] = propertyData;

            return true;
        }

        public static T Get<T>(this List<LayerPropertyData> properties) where T : LayerPropertyData
        {
            return properties.OfType<T>().FirstOrDefault();
        }
        
        public static IEnumerable<T> GetAll<T>(this List<LayerPropertyData> properties) where T : LayerPropertyData
        {
            return properties.OfType<T>();
        }
        
        public static T GetDefaultStylingPropertyData<T>(this List<LayerPropertyData> properties) where T : StylingPropertyData
        {
            return properties.OfType<T>().FirstOrDefault(data => data.StyleName == StylingPropertyData.NameOfDefaultStyle);
        }

        public static IEnumerable<T> FindAll<T>(this List<LayerPropertyData> properties) where T : class
        {
            return properties.OfType<T>();
        }

        public static bool Contains<T>(this List<LayerPropertyData> properties) where T : LayerPropertyData
        {
            return properties.OfType<T>().Any();
        }

        public static LayerPropertyData Get(this List<LayerPropertyData> properties, Type type) 
        {
            return properties.FirstOrDefault(data => data.GetType() == type);
        }
    }
}