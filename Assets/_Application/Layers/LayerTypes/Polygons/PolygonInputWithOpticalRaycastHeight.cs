using UnityEngine;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.Samplers;

namespace Netherlands3D.Twin.Layers.LayerTypes.Polygons
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