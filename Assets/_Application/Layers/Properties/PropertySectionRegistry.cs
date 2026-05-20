using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public readonly struct RegisteredPropertySectionType
    {
        public readonly Type SectionType;
        public readonly int Order;

        public RegisteredPropertySectionType(Type sectionType, int order)
        {
            SectionType = sectionType;
            Order = order;
        }
    }
    
    public class PropertySectionTypeCollection
    {
        public Dictionary<Type, List<RegisteredPropertySectionType>> Collection = new ();
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
                        
                        var entry = new RegisteredPropertySectionType(type, attr.Order);
                        
                        if (TypeRegistry[attr.Category].Collection.ContainsKey(attr.RequiredPropertyType))
                        {
                            TypeRegistry[attr.Category].Collection[attr.RequiredPropertyType].Add(entry);
                        }
                        else
                        {
                            TypeRegistry[attr.Category].Collection.Add(attr.RequiredPropertyType, new() { entry });
                        }
                    }
                }
            }
            foreach (var collection in TypeRegistry.Values)
                foreach (var list in collection.Collection.Values)
                    list.Sort((a, b) => a.Order.CompareTo(b.Order));
        }
    }
}