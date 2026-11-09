using System.Runtime.InteropServices;
using UnityEngine;

namespace Netherlands3D
{
    public static class WebGLClipboard
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void CopyToClipboard(string text);
#endif

        public static void Copy(string text)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CopyToClipboard(text);
#else
            GUIUtility.systemCopyBuffer = text;
#endif
        }
    }
}