using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using RuntimeHandle;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class TransformAxes : MonoBehaviour, IVisualizationWithPropertyData
    {
        [Header("Locks")]
        [Space(5)]
        public bool PositionLocked = false;
        public bool RotationLocked = false;
        public bool ScaleLocked = false;

        [Header("Allowed axes")]
        [Space(5)]
        public HandleAxes positionAxes = HandleAxes.XYZ;
        public HandleAxes rotationAxes = HandleAxes.XYZ;
        public HandleAxes scaleAxes = HandleAxes.XYZ;

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            var propertyData = (TransformLockLayerPropertyData)properties.FirstOrDefault(p => p is TransformLockLayerPropertyData);           
            if (propertyData == null)
            {
                propertyData = new TransformLockLayerPropertyData(PositionLocked ? 0 : (int)positionAxes, RotationLocked ? 0 : (int)rotationAxes, ScaleLocked ? 0 : (int)scaleAxes);                
                GetComponent<LayerGameObject>().LayerData.SetProperty(propertyData);
            }
        }
    }
}
