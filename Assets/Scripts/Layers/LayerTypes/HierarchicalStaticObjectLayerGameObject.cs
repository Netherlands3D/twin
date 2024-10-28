using UnityEngine;
using Netherlands3D.Coordinates;

namespace Netherlands3D.Twin.Layers
{
    public class HierarchicalStaticObjectLayerGameObject : HierarchicalObjectLayerGameObject
    {
        public Vector3 Rotation = Vector3.zero; //TODO get this value from original template
        public Vector3 Scale = Vector3.one; //TODO get this value from original template
        public float TargetHeight;
        public Vector2 Coordinates = Vector2.zero;
        [SerializeField] private CoordinateSystem coordinateSystem;

        protected override void InitializeCoordinates()
        {
            coord = new Coordinate(coordinateSystem, new double[3] { Coordinates.y, Coordinates.x, 0 });            
        }

        protected override void Start()
        {
            base.Start();
            UpdatePosition(coord);
            UpdateRotation(Rotation);
            UpdateScale(Scale);
        }

        protected override void UpdatePosition(Coordinate newPosition)
        {
            base.UpdatePosition(newPosition);
            transform.position = new Vector3(transform.position.x, TargetHeight, transform.position.z);
        }

        public override void OnSelect()
        {
         
        }

        public override void OnDeselect()
        {
         
        }
    }
}
