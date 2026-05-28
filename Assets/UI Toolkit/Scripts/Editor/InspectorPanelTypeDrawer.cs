using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(InspectorPanelType))]
public class InspectorPanelTypeDrawer : PropertyDrawer
{
    // Cached list + the frame it was built on (refresh once per domain reload)
    private static List<Type> _cachedPanelTypes;
    private static string[]   _cachedDisplayNames;
    private static string[]   _cachedAQNames; // AssemblyQualifiedName per entry (index 0 = "(None)")

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EnsureCache();

        var nameProp = property.FindPropertyRelative("_assemblyQualifiedName");
        string currentAQN = nameProp.stringValue;

        // Find current index in our cached list (0 = "(None)")
        int currentIndex = 0;
        if (!string.IsNullOrEmpty(currentAQN))
        {
            int found = Array.IndexOf(_cachedAQNames, currentAQN);
            currentIndex = found >= 0 ? found : 0;
        }

        EditorGUI.BeginProperty(position, label, property);

        int newIndex = EditorGUI.Popup(position, label.text, currentIndex, _cachedDisplayNames);

        if (newIndex != currentIndex)
        {
            nameProp.stringValue = newIndex == 0 ? string.Empty : _cachedAQNames[newIndex];
            property.serializedObject.ApplyModifiedProperties();
        }

        EditorGUI.EndProperty();
    }
    
    private static void EnsureCache()
    {
        if (_cachedPanelTypes != null) return;

        _cachedPanelTypes = new List<Type>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.GetCustomAttribute<InspectorPanelAttribute>() != null)
                        _cachedPanelTypes.Add(type);
                }
            }
            catch (ReflectionTypeLoadException e)
            {
                // Some assemblies may fail to fully load; skip broken types
                foreach (var type in e.Types)
                {
                    if (type == null) continue;
                    if (type.GetCustomAttribute<InspectorPanelAttribute>() != null)
                        _cachedPanelTypes.Add(type);
                }
            }
        }

        _cachedPanelTypes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

        // Index 0 means nothing selected
        _cachedDisplayNames = new[] { "(None)" }
            .Concat(_cachedPanelTypes.Select(t => t.Name))
            .ToArray();

        _cachedAQNames = new[] { string.Empty }
            .Concat(_cachedPanelTypes.Select(t => t.AssemblyQualifiedName))
            .ToArray();
    }

    // Bust the cache whenever scripts recompile so new [InspectorPanel] classes appear automatically
    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded() => _cachedPanelTypes = null;
}
