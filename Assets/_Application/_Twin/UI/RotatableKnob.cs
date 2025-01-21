using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Netherlands3D.Twin.UI
{
    public class RotatableKnob : MonoBehaviour, IDragHandler, IBeginDragHandler
    {
        [SerializeField] private RectTransform rotationCenter;
        private Vector2 initialMousePosition;
        private float initialRotation;

        public UnityEvent<float> onAngleChanged = new();
        public float Angle => rotationCenter.rotation.eulerAngles.z;
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            var localPoint = eventData.position - (Vector2)rotationCenter.position;
            initialMousePosition = localPoint;
        }

        public void OnDrag(PointerEventData eventData)
        {
            var localPoint = eventData.position - (Vector2)rotationCenter.position;
            
            // Calculate the angle between the initial and current mouse positions relative to the pivot point
            float angle = Mathf.Atan2(localPoint.y, localPoint.x) - Mathf.Atan2(initialMousePosition.y, initialMousePosition.x);
            angle *= Mathf.Rad2Deg;

            rotationCenter.Rotate(Vector3.forward, angle);
            initialMousePosition = localPoint;

            onAngleChanged.Invoke(Angle);
        }

        public void SetAngle(float angle)
        {
            rotationCenter.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
