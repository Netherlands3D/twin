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
        public PropertySection Prefab;
    }

    [CreateAssetMenu(fileName = "PropertyPanelRegistry", menuName = "Netherlands3D/PropertyPanelRegistry", order = 0)]
    public class PropertySectionRegistry : ScriptableObject
    {
        [SerializeField] private List<PropertyPanelEntry> Entries = new();

        private void OnValidate()
        {
            PropertySectionRegistryBuilder.Rebuild();
        }

        public void AddEntry(string typeName, PropertySection prefab)
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

        public PropertySection GetPrefab(Type type)
        {
            var entry = Entries.FirstOrDefault(e => e.TypeName == type.AssemblyQualifiedName);
            return entry.Prefab;
        }
        
    }
}