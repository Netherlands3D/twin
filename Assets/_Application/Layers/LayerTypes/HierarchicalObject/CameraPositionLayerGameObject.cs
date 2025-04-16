using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.Tools;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject
{
    public class CameraPositionLayerGameObject : HierarchicalObjectLayerGameObject
    {
        [SerializeField] private Color selectedColor;
        [SerializeField] private Tool layerTool;
        [SerializeField] private GameObject ghostGameObject;
        private Color defaultColor;
        CameraPropertyData cameraPropertyData => (CameraPropertyData)transformPropertyData;

        protected override void Awake()
        {
            base.Awake();
            var cam = Camera.main;
            var cameraPropertyData = new CameraPropertyData(new Coordinate(cam.transform.position), cam.transform.eulerAngles, cam.transform.localScale, cam.orthographic);
            cameraPropertyData.OnOrthographicChanged.AddListener(SetOrthographic);

            transformPropertyData = cameraPropertyData;
            defaultColor = GetComponentInChildren<MeshRenderer>().material.color;
            
            layerTool.onOpen.AddListener(EnableGhost);
            layerTool.onClose.AddListener(DisableGhost);
        }

        protected virtual void OnDestroy()
        {
            base.OnDestroy();
            cameraPropertyData.OnOrthographicChanged.RemoveListener(SetOrthographic);
            layerTool.onOpen.RemoveListener(EnableGhost);
            layerTool.onClose.RemoveListener(DisableGhost);
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
            //do not call base to not set transform handles
            foreach (var renderer in GetComponentsInChildren<MeshRenderer>())
            {
                renderer.material.color = selectedColor;
            }
        }

        public override void OnDeselect()
        {
            base.OnDeselect();
            foreach (var renderer in GetComponentsInChildren<MeshRenderer>())
            {
                renderer.material.color = defaultColor;
            }
        }

        private void EnableGhost()
        {
            ghostGameObject.SetActive(true);
        }

        private void DisableGhost()
        {
            ghostGameObject.SetActive(false);
        }
    }
}