using UnityEngine.InputSystem;
using UnityEngine;
using System;

namespace Netherlands3D.Twin.Samplers
{
    public class PointerToWorldPosition : MonoBehaviour
    {
        public Vector3 WorldPoint { get; private set; } //Constantly updated world point
        private OpticalRaycaster opticalRaycaster;
        private Action<Vector3> worldPointCallback;

        private void Awake()
        {
            opticalRaycaster = GetComponent<OpticalRaycaster>();
        }

        private void Start()
        {
            worldPointCallback = w => WorldPoint = w;
        }

        private void Update()
        {
            var screenPoint = Pointer.current.position.ReadValue();
            opticalRaycaster.GetWorldPointAsync(screenPoint, worldPointCallback);
        }
    }
}
