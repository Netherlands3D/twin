using System;
using UnityEngine;
using Netherlands3D.SelectionTools;
using UnityEngine.InputSystem;

namespace Netherlands3D.Twin
{
    public class PolygonInputWithOpticalRaycastHeight : PolygonInput
    {
        private PointerToWorldPosition pointerToWorldPosition;

        protected override void Awake()
        {
            base.Awake();
            pointerToWorldPosition = GameObject.FindAnyObjectByType<PointerToWorldPosition>();
        }

        protected override void UpdateCurrentWorldCoordinate()
        {
            var point = pointerToWorldPosition.WorldPoint;

            if (point != Vector3.zero)
                currentWorldCoordinate = point;
            else
                base.UpdateCurrentWorldCoordinate();
        }
    }
}