using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Netherlands3D.Twin.Editor
{
    public class DeveloperToolsWindow : EditorWindow
    {
        void OnGUI()
        {
            // Use the Object Picker to select the start SceneAsset
            EditorSceneManager.playModeStartScene = (SceneAsset)EditorGUILayout.ObjectField(
                new GUIContent("Start Scene"), EditorSceneManager.playModeStartScene, typeof(SceneAsset),
                false
            );
        }

        [MenuItem("Netherlands3D/Developer Tools")]
        static void Open()
        {
            GetWindow<DeveloperToolsWindow>();
        }
    }
}