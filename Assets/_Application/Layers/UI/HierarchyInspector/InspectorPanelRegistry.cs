using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Netherlands3D.UI.Behaviours
{
    public class InspectorPanelTypeCollection
    {
        public Dictionary<Type, List<Type>> Collection = new Dictionary<Type, List<Type>>();
    }

    public static class InspectorPanelRegistry
    {
        public static InspectorPanelTypeCollection TypeRegistry = new();

        static InspectorPanelRegistry()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type
                            .GetCustomAttributes(typeof(InspectorPanelAttribute), false)
                            .FirstOrDefault() is InspectorPanelAttribute attr)
                    {
                        if (type.IsNested) continue; //The [UxmlElement] attribute causes Unity to code-generate a nested UxmlSerializedData class inside the panel classes, and that nested class inherits the attributes (PropertySectionAttribute) of its parent, so it will be picked up twice here.
                      
                        if (TypeRegistry.Collection.ContainsKey(attr.RequiredInspectorType))
                        {
                            TypeRegistry.Collection[attr.RequiredInspectorType].Add(type);
                        }
                        else
                        {
                            TypeRegistry.Collection.Add(attr.RequiredInspectorType, new List<Type>() { type });
                        }
                    }
                }
            }
        }
    }
}