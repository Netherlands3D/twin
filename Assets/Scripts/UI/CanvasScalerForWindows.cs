using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.UI;

namespace Netherlands3D.Twin.UI
{
    /// <summary>
    /// This class is used to handle the display differences between Windows and MacOS.
    /// The main issue arises from the difference in Pixels Per Inch (PPI) between the two operating systems.
    /// The points in the canvas appear to be of different sizes on MacOS and Windows. 
    /// Therefore, we have to scale the canvas specifically on Windows to counteract this difference.
    /// </summary>
    public class CanvasScalerForWindows : MonoBehaviour
    {
        public Canvas canvas;
        public float factor = 0.75f;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern bool IsWindowsOS();
#endif

        void Start()
        {
            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
            }

            // Adjust the scale only if we're running in a WebGL build on Windows
#if UNITY_WEBGL && !UNITY_EDITOR
            ScaleCanvasForWindows();
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        // This method is used to scale the canvas on Windows.
        // The canvas is scaled down by the factor in this class to account for the PPI difference.
        void ScaleCanvasForWindows()
        {
            Debug.Log("Are we on Windows? " + (IsWindowsOS() ? "Yes" : "No"));
            if (!IsWindowsOS()) return;

            canvas.scaleFactor *= factor;
        }
#endif
    }
}