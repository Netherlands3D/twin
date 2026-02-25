using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI
{
    public class UiDpiNormalizer : MonoBehaviour
    {
        [SerializeField] private UIDocument doc;
#pragma warning disable 0414
        [Tooltip("The factor to adjust the scaling with on Windows, should generally be .75 to represent the difference between Mac and Windows DPI")]
        [SerializeField] private float factor = 0.75f;
#pragma warning restore 0414

#if UNITY_WEBGL && !UNITY_EDITOR
        private float previousScaleFactor = 0f;

        [DllImport("__Internal")]
        private static extern bool IsWindowsOS();
#endif

        void Start()
        {
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
            if (!IsWindowsOS()) return;

            doc.runtimePanel.panelSettings.scale *= factor;
            previousScaleFactor = doc.runtimePanel.panelSettings.scale;
            Debug.Log("Detected zooming of the canvas, readjusting scale factor");
        }
#endif

        private void Update()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (Mathf.Approximately(doc.runtimePanel.panelSettings.scale, previousScaleFactor) == false)
            {
                ScaleCanvasForWindows();
            }
#endif
        }
    }
}