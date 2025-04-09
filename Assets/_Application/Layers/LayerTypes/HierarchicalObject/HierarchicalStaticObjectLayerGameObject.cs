using UnityEngine;
using Netherlands3D.Coordinates;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject
{
    public class HierarchicalStaticObjectLayerGameObject : HierarchicalObjectLayerGameObject
    {
        public Vector3 Coordinates = Vector3.zero;
        [SerializeField] private CoordinateSystem coordinateSystem;


        protected override void Start()
        {
            base.Start();
            transformPropertyData.Position = new Coordinate(coordinateSystem, Coordinates.y, Coordinates.x, Coordinates.z);
            transformPropertyData.EulerRotation = transform.rotation.eulerAngles;
            transformPropertyData.LocalScale = transform.localScale;
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