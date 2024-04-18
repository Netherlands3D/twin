using System;
using UnityEngine;
using Netherlands3D.SelectionTools;
using UnityEngine.InputSystem;

namespace Netherlands3D.Twin
{
    public class PolygonInputWithOpticalRaycastHeight : PolygonInput
    {
        private OpticalRaycaster opticalRaycaster;

        protected override void Awake()
        {
            base.Awake();
            opticalRaycaster = GameObject.FindAnyObjectByType<OpticalRaycaster>();
        }

        protected override void UpdateCurrentWorldCoordinate()
        {
            var currentPointerPosition = pointerAction.ReadValue<Vector2>();
            var point = opticalRaycaster.GetWorldPointAtCameraScreenPoint(Camera.main, currentPointerPosition);

            if (point != Vector3.right) //todo: why is the default value (1,0,0) instead of (0,0,0)?
                currentWorldCoordinate = point;
            else
                base.UpdateCurrentWorldCoordinate();
        }
    }
}