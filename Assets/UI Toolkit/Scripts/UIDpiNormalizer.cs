using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI
{
    public class UiDpiNormalizer : MonoBehaviour
    {
        [SerializeField] private UIDocument doc;
        [SerializeField] private float targetDpi = 72f; // what you want it to look like
        [SerializeField] private float fallbackDpi = 96f; // used when Screen.dpi <= 0

        private void Awake()
        {
            var ps = doc.panelSettings;
            ps.scaleMode = PanelScaleMode.ConstantPixelSize;
            ps.referenceDpi = fallbackDpi;
            ps.fallbackDpi = fallbackDpi;

            Debug.Log("Target DPI: " + targetDpi);
            Debug.Log("Fallback DPI: " + fallbackDpi);
            Debug.Log("Screen DPI: " + Screen.dpi);
            
            // var reported = Screen.dpi > 0 ? Screen.dpi : fallbackDpi;
            // ps.scale = targetDpi / reported; // e.g., Win(96) => 0.75, Mac(72) => 1.0
            // Debug.Log("UI Scale: " + ps.scale);
        }
    }
}