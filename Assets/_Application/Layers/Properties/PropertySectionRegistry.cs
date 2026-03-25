using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public static class PropertySectionRegistry //todo: rename this when we can delete the old Registry
    {
        public static Dictionary<Type, Type> TypeRegistry = new Dictionary<Type, Type>();
            
        static PropertySectionRegistry()
        {
            var propertySectionTypes = TypeCache.GetTypesWithAttribute<PropertySectionAttribute>();
            foreach (var type in propertySectionTypes)
            {
                if (type
                        .GetCustomAttributes(typeof(PropertySectionAttribute), false)
                        .FirstOrDefault() is PropertySectionAttribute attr)
                {
                    if (type.IsNested) continue; //The [UxmlElement] attribute causes Unity to code-generate a nested UxmlSerializedData class inside the panel classes, and that nested class inherits the attributes (PropertySectionAttribute) of its parent, so it will be picked up twice here.
                    if (type.IsSubclassOf(typeof(MonoBehaviour))) continue; //todo: Remove this once all property panels are converted
                    
                    Debug.Log("adding type: " + attr.RequiredPropertyType + "\tpanel:" + type);
                    TypeRegistry.Add(attr.RequiredPropertyType, type);
                }
            }
        }
    }
}