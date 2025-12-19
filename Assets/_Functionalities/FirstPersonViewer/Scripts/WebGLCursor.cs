using Netherlands3D;
using System.Runtime.InteropServices;
using UnityEngine;

public static class WebGLCursor
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void LockCursorInternal();
    [DllImport("__Internal")] private static extern void UnlockCursorInternal();
#endif

    public static void Lock()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!WebGLOsDetection.IsMobile()) 
        {
            LockCursorInternal();
        }
#else
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
#endif
    }

    public static void Unlock()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!WebGLOsDetection.IsMobile())
        {
            UnlockCursorInternal();
        }
#else
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
#endif
    }
}
