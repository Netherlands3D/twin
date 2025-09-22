using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerTool : MonoBehaviour
    {
        [SerializeField] private RectTransform panelPrefab;
        [SerializeField] private ViewerToolbar toolbar;

        [Header("Button")]
        [SerializeField] private GameObject buttonRegular;
        [SerializeField] private GameObject buttonSelected;

        private void OnEnable()
        {
            toolbar.OnViewerToolChanged += ViewToolChanged;
        }

        private void OnDisable()
        {
            toolbar.OnViewerToolChanged -= ViewToolChanged;   
        }

        public void OnClick()
        {
            toolbar.OpenWindow(panelPrefab, this);
        }

        private void ViewToolChanged(ViewerTool viewTool)
        {
            bool isToolSelf = viewTool == this;
            buttonRegular.SetActive(!isToolSelf);
            buttonSelected.SetActive(isToolSelf);
        }
    }
}
