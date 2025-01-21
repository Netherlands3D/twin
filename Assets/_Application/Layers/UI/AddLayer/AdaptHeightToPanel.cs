using Netherlands3D.Twin.ExtensionMethods;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.UI.AddLayer
{
    public class AdaptHeightToPanel : MonoBehaviour
    {
        private RectTransform rectTransform;
        [SerializeField] private AddLayerPanel addLayerPanel;
        private float initialBottom;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            initialBottom =  rectTransform.offsetMin.y;
        }

        private void OnEnable()
        {
            addLayerPanel.OnRectTransformSizeChanged.AddListener(UpdateViewportHeight);
        }

        private void OnDisable()
        {
            addLayerPanel.OnRectTransformSizeChanged.RemoveListener(UpdateViewportHeight);
        }
        
        private void UpdateViewportHeight(float newHeight)
        {
            rectTransform.SetBottom(initialBottom + newHeight);
        }
    }
}
