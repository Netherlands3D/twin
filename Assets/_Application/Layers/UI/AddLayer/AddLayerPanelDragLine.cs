using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.UI.AddLayer
{
    public class AddLayerPanelDragLine : MonoBehaviour, IDragHandler, IEndDragHandler
    {
        [SerializeField] private AddLayerPanel panel;
        [SerializeField] private Toggle openToggle;
        private bool wasDragged = false;

        public void OnDrag(PointerEventData eventData)
        {
            panel.ResizePanel(eventData.delta.y);
            wasDragged = true;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            panel.EndResizeAction();
            wasDragged = false;
        }

        public void ToggleIsOnIfNotDragged()
        {
            if (!wasDragged)
                openToggle.isOn = !openToggle.isOn;
        }
    }
}