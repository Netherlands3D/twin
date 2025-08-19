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
        private int cullingMask = Physics.DefaultRaycastLayers;
        private float maxDistance = 10000;

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
                    Vector3 position = GetWorldPoint();
                    worldPoint = new Coordinate(position);
                }
            };
        }

        private void Update()
        {
            var screenPoint = Pointer.current.position.ReadValue();
            opticalRaycaster.GetWorldPointAsync(screenPoint, worldPointCallback, cullingMask);
        }

        public void GetPointerWorldPointAsync(Action<Vector3> result)
        {
            var screenPoint = Pointer.current.position.ReadValue();
            opticalRaycaster.GetWorldPointAsync(screenPoint, (point, hit) =>
            {
                if (hit)
                    result.Invoke(point);
                else
                {
                    Vector3 position = GetWorldPoint();
                    result.Invoke(position);
                }

            }, cullingMask);
        }

        public Vector3 GetWorldPoint()
        {
            var screenPoint = Pointer.current.position.ReadValue();
            Plane worldPlane = new Plane(Vector3.up, Vector3.zero);
            var screenRay = Camera.main.ScreenPointToRay(screenPoint);
            worldPlane.Raycast(screenRay, out float distance);
            Vector3 position;
            //when no valid point is found in for the raycast, lets invert the distance so we get a point in the sky
            if (distance < 0)
                position = screenRay.GetPoint(Mathf.Min(maxDistance, -distance));
            else
                position = screenRay.GetPoint(Mathf.Min(maxDistance, distance));
            return position;
        }
    }
}
