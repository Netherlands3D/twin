using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class PropertySectionRegistryBuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            // Automatically rebuild registry before the build
            PropertySectionRegistryBuilder.Rebuild(true); 
        }
        

// #if UNITY_EDITOR
//         // Auto-register play mode callback when editor loads
//         [InitializeOnLoadMethod]
//         private static void RegisterPlayModeCallback()
//         {
//             EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
//         }
//
//         private static void OnPlayModeStateChanged(PlayModeStateChange state)
//         {
//             // Trigger right before entering Play Mode
//             if (state == PlayModeStateChange.ExitingEditMode)
//             {
//                 Debug.Log("rebuilding");
//                 PropertyPanelRegistryBuilder.Rebuild(true);
//             }
//         }
// #endif
    }
}