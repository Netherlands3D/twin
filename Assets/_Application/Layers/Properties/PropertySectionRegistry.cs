using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class PropertySectionTypeCollection
    {
        public Dictionary<Type, List<Type>> Collection = new Dictionary<Type, List<Type>>();
    }
    
    public static class PropertySectionRegistry
    {
        public static Dictionary<PropertySectionCategory, PropertySectionTypeCollection> TypeRegistry = new();
            
        static PropertySectionRegistry()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type
                            .GetCustomAttributes(typeof(PropertySectionAttribute), false)
                            .FirstOrDefault() is PropertySectionAttribute attr)
                    {
                        if (type.IsNested) continue; //The [UxmlElement] attribute causes Unity to code-generate a nested UxmlSerializedData class inside the panel classes, and that nested class inherits the attributes (PropertySectionAttribute) of its parent, so it will be picked up twice here.
                        if (type.IsSubclassOf(typeof(MonoBehaviour))) continue; //todo: Remove this once all property panels are converted

                        if (!TypeRegistry.ContainsKey(attr.Category))
                        {
                            TypeRegistry.Add(attr.Category, new ());
                        }
                        
                        if (TypeRegistry[attr.Category].Collection.ContainsKey(attr.RequiredPropertyType))
                        {
                            TypeRegistry[attr.Category].Collection[attr.RequiredPropertyType].Add(type);
                        }
                        else
                        {
                            TypeRegistry[attr.Category].Collection.Add(attr.RequiredPropertyType, new List<Type>() { type });
                        }
                    }
                }
            }
        }
    }
}