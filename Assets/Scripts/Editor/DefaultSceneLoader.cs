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

            if (state == PlayModeStateChange.EnteredPlayMode && SceneManager.GetActiveScene().buildIndex != 0)
            {
                Debug.Log("Switching to ConfigLoader scene via DefaultSceneLoader.cs"); //added this log to make sure devs who don't know about this script know why their scene keeps switching when entering play mode
                EditorSceneManager.LoadScene(0);
            }
        }
    }
}
#endif