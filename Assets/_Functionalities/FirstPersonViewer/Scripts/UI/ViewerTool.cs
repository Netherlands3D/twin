using Netherlands3D.Events;
using Netherlands3D.Twin.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerTool : MonoBehaviour
    {
        [SerializeField] private RectTransform panelPrefab;
        [SerializeField] private ViewerToolbar toolbar;

        [Header("Button")]
        [SerializeField] private GameObject buttonRegular;
        [SerializeField] private GameObject buttonSelected;

        [Header("Events")]
        public UnityEvent OnToolSelected;
        public UnityEvent OnToolDeselected;

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
            OnToolSelected?.Invoke();
            toolbar.OpenWindow(panelPrefab, this);
        }

        private void ViewToolChanged(ViewerTool viewTool)
        {
            bool isToolSelf = viewTool == this;
            buttonRegular.SetActive(!isToolSelf);
            buttonSelected.SetActive(isToolSelf);
            
            if(!isToolSelf) OnToolDeselected?.Invoke();
        }
    }
}
