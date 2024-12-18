using Netherlands3D.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Netherlands3D.Twin
{
    public class MobileButton : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
    {
        private RaceController controller;
        private bool isPressed = false;
        public float moveValue = 0;

        private void Start()
        {
            controller = FindObjectOfType<RaceController>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
           isPressed = false;
        }

        private void Update()
        {
            if (controller != null && isPressed)
            {
                controller.MoveHorizontally(moveValue);
            }
        }
    }
}
