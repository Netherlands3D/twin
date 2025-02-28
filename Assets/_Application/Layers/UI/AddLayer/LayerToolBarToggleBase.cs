using Netherlands3D.Twin.Layers.UI.HierarchyInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.UI.AddLayer
{
    [RequireComponent(typeof(Toggle))]
    public abstract class LayerToolBarToggleBase : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] protected LayerUIManager layerUIManager;
        protected Toggle toggle;

        private void Awake()
        {
            toggle = GetComponent<Toggle>();
        }

        private void OnEnable()
        {
            toggle.onValueChanged.AddListener(ToggleAction);
        }

        private void OnDisable()
        {
            toggle.onValueChanged.RemoveListener(ToggleAction);
        }

        public abstract void ToggleAction(bool isOn);
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