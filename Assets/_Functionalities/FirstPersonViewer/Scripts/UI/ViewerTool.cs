using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerTool : MonoBehaviour
    {
        [SerializeField] private RectTransform panelPrefab;
        [SerializeField] private ViewerToolbar toolbar;

        public void OnClick()
        {
            toolbar.OpenWindow(panelPrefab);
        }
    }
}
