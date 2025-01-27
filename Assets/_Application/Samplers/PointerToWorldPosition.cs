using UnityEngine.InputSystem;
using UnityEngine;

namespace Netherlands3D.Twin.Samplers
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
            opticalRaycaster.GetWorldPointAtCameraScreenPoint(Camera.main, screenPoint, w =>
            {
                WorldPoint = w;
                Debug.Log(WorldPoint);
            });
            
        }
    }
}
