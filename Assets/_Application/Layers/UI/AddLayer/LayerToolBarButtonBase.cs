using Netherlands3D.Twin.Layers.UI.HierarchyInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.UI.AddLayer
{
    [RequireComponent(typeof(Button))]
    public abstract class LayerToolBarButtonBase : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] protected LayerUIManager layerUIManager;
        protected Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            button.onClick.AddListener(ButtonAction);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(ButtonAction);
        }

        public abstract void ButtonAction();
        public abstract void OnDrop(PointerEventData eventData);
        public void OnPointerEnter(PointerEventData eventData)
        {
            layerUIManager.MouseIsOverButton = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            layerUIManager.MouseIsOverButton = false;
        }
    }
}