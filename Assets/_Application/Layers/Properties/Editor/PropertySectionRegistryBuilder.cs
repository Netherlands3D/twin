using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public static class PropertySectionRegistryBuilder
    {
        public static PropertySectionRegistry Registry;
        
        [MenuItem("Tools/Rebuild Property UI Registry")]
        public static void Rebuild(bool log = true)
        {
            Registry = GetOrCreateRegistry();

            Registry.Clear();

            // Find all prefabs
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            Debug.Log("found " + prefabGuids.Length + " Prefabs");
            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<PropertySection>(path);
                if (prefab == null) continue;

                // Look for components with the PropertyUI attribute
                // var propertySection = prefab.GetComponent<PropertySection>(true);

                // foreach (var component in propertySection)
                // {
                    if (prefab == null)
                    {
                        Debug.Log(prefab.name + "has null component of type PropertySection");
                        continue;
                    }
                    if (prefab.GetType()
                            .GetCustomAttributes(typeof(PropertySectionAttribute), false)
                            .FirstOrDefault() is PropertySectionAttribute attr)
                    {
                        Debug.Log("found prefab " + prefab.name + " with property panel attribute for: " + attr.PropertyType + " with name: " + attr.PropertyType.AssemblyQualifiedName);
                        Registry.AddEntry(attr.PropertyType.AssemblyQualifiedName, prefab);
                    }
                // }
            }

            EditorUtility.SetDirty(Registry);
            AssetDatabase.SaveAssets();
            if (log)
                Debug.Log("Property Panel Registry rebuilt successfully.");
        }

        private static PropertySectionRegistry GetOrCreateRegistry()
        {
            // Search all assets of type PropertyPanelRegistry
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(PropertySectionRegistry));

            if (guids.Length > 0)
            {
                // Load the first match
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<PropertySectionRegistry>(path);
            }

            // If not found, create a new one somewhere reasonable
            const string defaultPath = "Assets/PropertySectionRegistry.asset";
            var registry = ScriptableObject.CreateInstance<PropertySectionRegistry>();
            AssetDatabase.CreateAsset(registry, defaultPath);

            return registry;
        }
    }
}