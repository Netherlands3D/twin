using System.Runtime.InteropServices;
using UnityEngine;

namespace Netherlands3D
{
    public static class WebGLOsDetection
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern bool IsWindowsOS();
        [DllImport("__Internal")] private static extern bool IsMobileDevice();
#endif


        public static bool IsWindows()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return IsWindowsOS();
#else 
            return true;
#endif
        }

        public static bool IsMobile()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return IsMobileDevice();
#else
            return false;
#endif
        }
    }
}
