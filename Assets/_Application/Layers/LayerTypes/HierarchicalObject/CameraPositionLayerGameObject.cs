using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject
{
    public class CameraPositionLayerGameObject : HierarchicalObjectLayerGameObject
    {
        CameraPropertyData cameraPropertyData => (CameraPropertyData)transformPropertyData;

        protected override void Awake()
        {
            base.Awake();
            var cam = Camera.main;
            var cameraPropertyData = new CameraPropertyData(new Coordinate(cam.transform.position), cam.transform.eulerAngles, cam.transform.localScale, cam.orthographic);
            cameraPropertyData.OnOrthographicChanged.AddListener(SetOrthographic);

            transformPropertyData = cameraPropertyData;
        }

        protected virtual void OnDestroy()
        {
            base.OnDestroy();
            cameraPropertyData.OnOrthographicChanged.RemoveListener(SetOrthographic);
        }

        public override void LoadProperties(List<LayerPropertyData> properties)
        {
            base.LoadProperties(properties);

            var cameraProperty = (CameraPropertyData)properties.FirstOrDefault(p => p is AnnotationPropertyData);
            if (cameraProperty != null)
            {
                if (cameraPropertyData != null) //unsubscribe events from previous property object, resubscribe to new object at the end of this if block
                {
                    cameraPropertyData.OnOrthographicChanged.RemoveListener(SetOrthographic);
                }

                transformPropertyData = cameraProperty; //take existing TransformProperty to overwrite the unlinked one of this class
                
                SetOrthographic(cameraProperty.Orthographic);

                cameraProperty.OnOrthographicChanged.AddListener(SetOrthographic);
            }
        }

        private void SetOrthographic(bool orthographic)
        {
            Camera.main.orthographic = orthographic;
        }

        protected override void OnDoubleClick(LayerData layer)
        {
            MoveCameraToView();
        }

        private void MoveCameraToView()
        {
            Camera.main.GetComponent<MoveCameraToCoordinate>().LoadCameraData(cameraPropertyData);
        }
        
        public override void OnSelect()
        {
            //do not set transform handles
        }
    }
}