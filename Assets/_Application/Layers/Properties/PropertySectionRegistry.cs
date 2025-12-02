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
        public string SubType;
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
        public void AddEntry(string typeName, GameObject prefab, string subType)
        {
            var entry = new PropertyPanelEntry();
            entry.TypeName = typeName;
            entry.Prefab = prefab;
            entry.SubType = subType;
            Entries.Add(entry);
        }

        public void Clear()
        {
            Entries.Clear();
        }

        public bool HasPanel(Type type, LayerPropertyData propertyData)
        {
            if(propertyData.CustomFlags != null && propertyData.CustomFlags.Count > 0)
            {
                foreach (var flag in propertyData.CustomFlags)
                {
                    if (Entries.Any(e => e.TypeName == type.AssemblyQualifiedName && e.SubType == flag))
                    {
                        return true;
                    }
                }
            }

            return Entries.Any(entry => entry.TypeName == type.AssemblyQualifiedName);
        }

        public List<GameObject> GetPanelPrefabs(Type type, LayerPropertyData propertyData)
        {
            List<GameObject> prefabs = new List<GameObject>();  
            if (propertyData.CustomFlags != null && propertyData.CustomFlags.Count > 0)
            {
                foreach (var flag in propertyData.CustomFlags)
                {
                    var entriesWithFlag = Entries.Where(e => e.TypeName == type.AssemblyQualifiedName && e.SubType == flag);
                    prefabs.AddRange(entriesWithFlag.Select(e => e.Prefab));
                }
                return prefabs;
            }

            var entry = Entries.FirstOrDefault(e => e.TypeName == type.AssemblyQualifiedName);
            if (entry != null)
            {
                foreach (var interfaceType in type.GetInterfaces())
                {
                    if (!HasPanel(interfaceType, propertyData)) continue;

                    entry = Entries.FirstOrDefault(e => e.TypeName == interfaceType.AssemblyQualifiedName);
                    break;
                }
                prefabs.Add(entry.Prefab);
            }
            return prefabs;
        }
    }
}