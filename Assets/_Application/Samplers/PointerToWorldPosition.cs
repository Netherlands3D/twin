using UnityEngine.InputSystem;
using UnityEngine;
using System;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Coordinates;

namespace Netherlands3D.Twin.Samplers
{
    public class PointerToWorldPosition : MonoBehaviour
    {
        public WorldTransform WorldTransformTarget;
        public Coordinate WorldPoint => WorldTransformTarget.Coordinate;
        private OpticalRaycaster opticalRaycaster;
        private Action<Vector3, bool> worldPointCallback;
        private bool lockUpdate = false;

        private void Awake()
        {
            opticalRaycaster = GetComponent<OpticalRaycaster>();
        }

        private void Start()
        {
            WorldTransformTarget.onPreShift.AddListener((w,c) => lockUpdate = true);
            WorldTransformTarget.onPostShift.AddListener((w, c) => lockUpdate = true);

            worldPointCallback = (w,h) =>
            {
                if (h && !lockUpdate)
                    WorldTransformTarget.MoveToCoordinate(new Coordinate(w));
                else
                {
                    //var screenPoint = Pointer.current.position.ReadValue();
                    //Plane worldPlane = new Plane(Vector3.up, Vector3.zero);
                    //var screenRay = Camera.main.ScreenPointToRay(screenPoint);
                    //worldPlane.Raycast(screenRay, out float distance);
                    //WorldTransformTarget.MoveToCoordinate(new Coordinate(screenRay.GetPoint(Mathf.Min(float.MaxValue, distance))));
                }
            };
        }

        private void Update()
        {
            if (lockUpdate)
                return;

            var screenPoint = Pointer.current.position.ReadValue();
            opticalRaycaster.GetWorldPointAsync(screenPoint, worldPointCallback);
        }
    }
}
