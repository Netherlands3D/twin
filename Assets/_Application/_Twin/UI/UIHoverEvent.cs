using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Netherlands3D
{
    public class UIHoverEvent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public UnityEvent OnStartHover;
        public UnityEvent OnEndHover;
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            OnStartHover.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnEndHover.Invoke();
        }
    }
}