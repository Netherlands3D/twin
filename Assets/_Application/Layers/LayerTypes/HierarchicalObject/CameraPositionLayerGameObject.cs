using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.Tools;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject
{
    public class CameraPositionLayerGameObject : HierarchicalObjectLayerGameObject
    {
        [SerializeField] private Material ghostMaterial;
        [SerializeField] private Color selectedColor;
        [SerializeField] private Tool layerTool;
        [SerializeField] private GameObject ghostGameObject;
        private Color defaultColor;
        private CameraPropertyData cameraPropertyData => LayerData.GetProperty<CameraPropertyData>();
        public override bool IsMaskable => false;

        protected override void OnLayerInitialize()
        {
            base.OnLayerInitialize();
            cameraPropertyData.OnOrthographicChanged.AddListener(SetOrthographic);

            defaultColor = ghostMaterial.color;
            
            layerTool.onOpen.AddListener(EnableGhost);
            layerTool.onClose.AddListener(DisableGhost);
        }

        protected override void InitializePropertyData()
        {
            if (cameraPropertyData != null) return;

            var cam = Camera.main;
            var camTransform = cam.transform;

            LayerData.SetProperty(
                new CameraPropertyData(
                    new Coordinate(camTransform.position), 
                    camTransform.eulerAngles, 
                    camTransform.localScale, 
                    cam.orthographic
                )
            );
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            cameraPropertyData.OnOrthographicChanged.RemoveListener(SetOrthographic);
            layerTool.onOpen.RemoveListener(EnableGhost);
            layerTool.onClose.RemoveListener(DisableGhost);
        }

        public override void LoadProperties(List<LayerPropertyData> properties)
        {
            base.LoadProperties(properties);

            SetOrthographic(cameraPropertyData.Orthographic);
        }

        private void SetOrthographic(bool orthographic)
        {
            Camera.main.orthographic = orthographic;
        }

        protected override void OnDoubleClick(LayerData layer)
        {
            MoveCameraToSavedLocation();
        }

        private void MoveCameraToSavedLocation()
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