using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
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