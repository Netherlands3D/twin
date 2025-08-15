using UnityEngine.InputSystem;
using UnityEngine;
using System;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Coordinates;

namespace Netherlands3D.Twin.Samplers
{
    public class PointerToWorldPosition : MonoBehaviour
    {       
        public Coordinate WorldPoint => worldPoint;

        private OpticalRaycaster opticalRaycaster;
        private Action<Vector3, bool> worldPointCallback;
        private Coordinate worldPoint;
        private int cullingMask = Physics.DefaultRaycastLayers & ~(1 << 0); //lets exclude the ground plane, so we get a better position on a failed optical raycast

        private void Awake()
        {
            opticalRaycaster = GetComponent<OpticalRaycaster>();
        }

        private void Start()
        {
            worldPointCallback = (w,h) =>
            {
                if (h)
                    worldPoint = new Coordinate(w);
                else
                {
                    var screenPoint = Pointer.current.position.ReadValue();
                    Plane worldPlane = new Plane(Vector3.up, Vector3.zero);
                    var screenRay = Camera.main.ScreenPointToRay(screenPoint);
                    worldPlane.Raycast(screenRay, out float distance);
                    worldPoint = new Coordinate(screenRay.GetPoint(Mathf.Min(float.MaxValue, distance)));
                }
            };
        }

        private void Update()
        {
            var screenPoint = Pointer.current.position.ReadValue();
            opticalRaycaster.GetWorldPointAsync(screenPoint, worldPointCallback, cullingMask);
        }
    }
}
