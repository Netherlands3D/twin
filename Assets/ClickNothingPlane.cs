using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin
{
    public class ClickNothingPlane : MonoBehaviour, IPointerClickHandler
    {
        private Camera mainCam;
        public static UnityEvent ClickedOnNothing = new();
        public int offset = 1000;
        void Start()
        {
            mainCam = Camera.main;
        }

        void Update()
        {
            var maxDistance = mainCam.farClipPlane - offset;
            
            transform.position = mainCam.transform.position + mainCam.transform.forward * maxDistance;
            transform.rotation = mainCam.transform.rotation;
            transform.Rotate(Vector3.right, -90f, Space.Self);
            float planeHeight = Mathf.Tan(Mathf.Deg2Rad * mainCam.fieldOfView / 2f) * 2f * maxDistance;
            float planeWidth = planeHeight * mainCam.aspect;
            transform.localScale = new Vector3(planeWidth, 1f, planeHeight);
            // transform.localScale = Vector3.one * mainCam.farClipPlane;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            ClickedOnNothing.Invoke();
        }
    }
}
