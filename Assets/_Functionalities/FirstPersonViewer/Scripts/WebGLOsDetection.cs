using System.Runtime.InteropServices;
using UnityEngine;

namespace Netherlands3D
{
    public static class WebGLOsDetection
    {
        [DllImport("__Internal")] private static extern bool IsWindowsOS();
        [DllImport("__Internal")] private static extern bool IsMobileDevice();

        public static bool IsWindows() => IsWindowsOS();
        public static bool IsMobile() => IsMobileDevice();
    }
}
