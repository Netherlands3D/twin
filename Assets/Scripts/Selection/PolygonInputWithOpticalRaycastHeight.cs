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
            var point = opticalRaycaster.WorldPoint;

            if (point != Vector3.zero)
                currentWorldCoordinate = point;
            else
                base.UpdateCurrentWorldCoordinate();
        }
    }
}