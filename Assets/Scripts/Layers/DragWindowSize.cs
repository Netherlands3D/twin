using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Netherlands3D.Twin
{
    public class DragWindowSize : MonoBehaviour, IDragHandler, IEndDragHandler
    {
        [SerializeField] private AddLayerPanel panel;
        public void OnDrag(PointerEventData eventData)
        {
            panel.ResizePanel(eventData.delta.y);
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            panel.EndResizeAction();
        }
    }
}
