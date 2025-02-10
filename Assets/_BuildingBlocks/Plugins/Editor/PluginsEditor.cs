using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Netherlands3D.Plugins
{
    [CustomEditor(typeof(Plugins))]
    public class PluginsEditor : Editor
    {
        private Plugins plugins;
        private Vector2 scrollPosition;

        private void OnEnable()
        {
            plugins = (Plugins)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Loaded Plugins", EditorStyles.boldLabel);

            if (this.plugins == null)
            {
                EditorGUILayout.HelpBox("PluginManager instance not found.", MessageType.Warning);
                return;
            }

            List<PluginManifest> plugins = new List<PluginManifest>(this.plugins.GetAllPlugins());

            if (plugins.Count == 0)
            {
                EditorGUILayout.HelpBox("No plugins registered.", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

            foreach (var plugin in plugins)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Plugin Name:", plugin.displayName, EditorStyles.boldLabel);
                EditorGUILayout.ObjectField("Manifest:", plugin, typeof(PluginManifest), false);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }
    }

}