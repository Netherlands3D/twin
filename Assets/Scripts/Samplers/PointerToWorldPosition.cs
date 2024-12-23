using UnityEngine.InputSystem;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class PointerToWorldPosition : MonoBehaviour
    {
        public Vector3 WorldPoint { get; private set; } //Constantly updated world point
        private OpticalRaycaster opticalRaycaster;

        private void Awake()
        {
            opticalRaycaster = GetComponent<OpticalRaycaster>();
        }

        private void Update()
        {
            var screenPoint = Pointer.current.position.ReadValue();
            WorldPoint = opticalRaycaster.GetWorldPointAtCameraScreenPoint(Camera.main, screenPoint);
        }
    }
}
