using UnityEngine;
using Netherlands3D.Coordinates;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject
{
    public class HierarchicalStaticObjectLayerGameObject : HierarchicalObjectLayerGameObject
    {
        public Vector3 Rotation = Vector3.zero; //TODO get this value from original template
        public Vector3 Scale = Vector3.one; //TODO get this value from original template
        public float TargetHeight;
        public Vector3 Coordinates = Vector3.zero;
        [SerializeField] private CoordinateSystem coordinateSystem;


        protected override void Start()
        {
            base.Start();
            transformPropertyData.Position = new Coordinate(coordinateSystem, Coordinates.y, Coordinates.x, Coordinates.z);
            transformPropertyData.EulerRotation = Rotation;
            transformPropertyData.LocalScale = Scale;
        }

        protected override void UpdatePosition(Coordinate newPosition)
        {
            base.UpdatePosition(newPosition);
            transform.position = new Vector3(transform.position.x, TargetHeight, transform.position.z);
        }

        protected override void UpdateRotation(Vector3 newAngles)
        {
            base.UpdateRotation(newAngles);
            transform.rotation = Quaternion.Euler(Rotation);
        }

        public override void OnSelect()
        {
            //this is to prevent executing base class functionality
        }

        public override void OnDeselect()
        {
            //this is to prevent executing base class functionality
        }
    }
}
