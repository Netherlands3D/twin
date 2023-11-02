using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class DragGhost : MonoBehaviour//, IPointerDownHandler, IPointerUpHandler
    {
        private Vector2 DragStartOffset { get; set; }

        public void Initialize(Vector2 dragStartOffset)
        {
            // transform.position = startPosition;
            // var pointerPosition = Pointer.current.position.ReadValue();
            DragStartOffset = dragStartOffset;
            CalculateNewPosition();
        }

        void Update()
        {
            CalculateNewPosition();
        }

        private void CalculateNewPosition()
        {
            var pointerPosition = Pointer.current.position.ReadValue();
            var newPos = new Vector2(transform.position.x, pointerPosition.y + DragStartOffset.y);
            transform.position = newPos;
        }

        // public void OnPointerDown(PointerEventData eventData)
        // {
        // }        
        //
        // public void OnPointerUp(PointerEventData eventData)
        // {
        //     Destroy(gameObject);
        // }
    }
}