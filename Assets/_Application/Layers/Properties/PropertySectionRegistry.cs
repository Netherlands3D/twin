using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public static class PropertySectionRegistry2 //todo: rename this when we can delete the old Registry
    {
        public static Dictionary<Type, Type> TypeRegistry = new Dictionary<Type, Type>();
            
        static PropertySectionRegistry2()
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
    
    [Serializable]
    public class PropertyPanelEntry
    {
        public string TypeName;
        public GameObject Prefab;
    }

    [CreateAssetMenu(fileName = "PropertyPanelRegistry", menuName = "Netherlands3D/PropertyPanelRegistry", order = 0)]
    public class PropertySectionRegistry : ScriptableObject
    {
        [SerializeField] private List<PropertyPanelEntry> Entries = new();
#if UNITY_EDITOR
        private void OnValidate()
        {
            PropertySectionRegistryBuilder.Rebuild();
        }
#endif
        public void AddEntry(string typeName, GameObject prefab)
        {
            var entry = new PropertyPanelEntry();
            entry.TypeName = typeName;
            entry.Prefab = prefab;
            Entries.Add(entry);
        }

        public void Clear()
        {
            Entries.Clear();
        }

        public bool HasPanel(Type type)
        {
            return Entries.Any(entry => entry.TypeName == type.AssemblyQualifiedName);
        }

        public List<GameObject> GetPanelPrefabs(Type type, LayerPropertyData propertyData)
        {
            List<GameObject> prefabs = new List<GameObject>();  
            foreach(var entry in Entries)
            {
                if (entry.TypeName == type.AssemblyQualifiedName)
                {
                    prefabs.Add(entry.Prefab);
                }
            }

            foreach (var interfaceType in type.GetInterfaces())
            {
                if (!HasPanel(interfaceType))
                    continue;

                foreach (var entry in Entries)
                {
                    if (entry.TypeName == interfaceType.AssemblyQualifiedName)
                    {
                        prefabs.Add(entry.Prefab);
                    }
                }
            }
            return prefabs;
        }
    }
}