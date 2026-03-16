#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Netherlands3D.Tiles3D.Editor
{
    [InitializeOnLoad]
    internal static class CaptureGlbUrlAndPositionMenu
    {
        private const string PrefKey = "Netherlands3D.Tiles3D.EnableTransformCapture";
        private const string MenuRoot = "Netherlands3D/GLB URL + Position Capture/";
        private const string AutoDisableMessage = "GLB URL + position capture disabled (Play Mode ended).";

        static CaptureGlbUrlAndPositionMenu()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem(MenuRoot + "Start Capturing", priority = 0)]
        private static void EnableCapture() => SetCapture(true, "GLB URL + position capture enabled.");

        [MenuItem(MenuRoot + "Start Capturing", validate = true)]
        private static bool ValidateEnable() => !EditorPrefs.GetBool(PrefKey, false);

        [MenuItem(MenuRoot + "Stop Capturing", priority = 1)]
        private static void DisableCapture() => SetCapture(false, "GLB URL + position capture disabled.");

        [MenuItem(MenuRoot + "Stop Capturing", validate = true)]
        private static bool ValidateDisable() => EditorPrefs.GetBool(PrefKey, false);

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode && EditorPrefs.GetBool(PrefKey, false))
            {
                SetCapture(false, AutoDisableMessage);
            }
        }

        private static void SetCapture(bool enable, string logMessage)
        {
            EditorPrefs.SetBool(PrefKey, enable);
            if (!string.IsNullOrEmpty(logMessage))
            {
                Debug.Log(logMessage);
            }
        }
    }
}
#endif
