#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Netherlands3D.Twin.Editor
{
    ///
    /// Shamelessly stolen from https://stackoverflow.com/a/48817315
    /// 
    [InitializeOnLoad]
    public static class DefaultSceneLoader
    {
        static DefaultSceneLoader()
        {
            EditorApplication.playModeStateChanged += LoadDefaultScene;
        }

        static void LoadDefaultScene(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            }

            if (state != PlayModeStateChange.EnteredPlayMode) return;
            if (SceneManager.GetActiveScene().buildIndex == 0) return;
            if (IsRunningPlayModeTests()) return;

            //added this log to make sure devs who don't know about this script know why their scene keeps switching when entering play mode
            Debug.Log("Switching to ConfigLoader scene via DefaultSceneLoader.cs"); 
            SceneManager.LoadScene(0);
        }

        private static bool IsRunningPlayModeTests()
        {
            return SceneManager.GetActiveScene().name.StartsWith("InitTestScene");
        }
    }
}
#endif