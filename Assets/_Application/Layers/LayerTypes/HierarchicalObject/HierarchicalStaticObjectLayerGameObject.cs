using UnityEngine;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers.Properties;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject
{
    public class HierarchicalStaticObjectLayerGameObject : HierarchicalObjectLayerGameObject
    {
        public Vector3 Coordinates = Vector3.zero;
        [SerializeField] private CoordinateSystem coordinateSystem;

        protected override void OnVisualizationReady()
        {
            base.OnVisualizationReady();
            var transformProperty = LayerData.GetProperty<TransformLayerPropertyData>();
            transformProperty.Position = new Coordinate(coordinateSystem, Coordinates.y, Coordinates.x, Coordinates.z);
            transformProperty.EulerRotation = transform.rotation.eulerAngles;
            transformProperty.LocalScale = transform.localScale;
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