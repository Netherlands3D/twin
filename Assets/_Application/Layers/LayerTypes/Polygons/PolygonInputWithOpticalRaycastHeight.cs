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
            pointerToWorldPosition = FindAnyObjectByType<PointerToWorldPosition>();
        }

        public override void SetSelectionCurrentPosition(Vector3 position)
        {
            var point = pointerToWorldPosition.WorldPoint.ToUnity();
            if (point != Vector3.zero)
                base.SetSelectionCurrentPosition(point);
            else
                base.SetSelectionCurrentPosition(selectionCurrentPosition);
        }
    }
}