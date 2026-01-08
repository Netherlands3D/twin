using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.Tools;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;
using Netherlands3D.Twin.Layers.ExtensionMethods;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject
{
    public class CameraPositionLayerGameObject : HierarchicalObjectLayerGameObject
    {
        [SerializeField] private Material ghostMaterial;
        [SerializeField] private Color selectedColor;
        [SerializeField] private Tool layerTool;
        [SerializeField] private GameObject ghostGameObject;
        private Color defaultColor;
        
        protected override void OnVisualizationInitialize()
        {
            base.OnVisualizationInitialize();
            defaultColor = ghostMaterial.color;
            
            layerTool.onOpen.AddListener(EnableGhost);
            layerTool.onClose.AddListener(DisableGhost);
        }

        public override void LoadProperties(List<LayerPropertyData> properties)
        {
            base.LoadProperties(properties);
            InitProperty<CameraPropertyData>(properties, null, new Coordinate(Camera.main.transform.position),
                    Camera.main.transform.eulerAngles,
                    Camera.main.transform.localScale,
                    Camera.main.orthographic);
            

            SetOrthographic(LayerData.GetProperty<CameraPropertyData>().Orthographic);
        }

        protected override void RegisterEventListeners()
        {
            base.RegisterEventListeners();
            var cameraPropertyData = LayerData.GetProperty<CameraPropertyData>();
            cameraPropertyData.OnOrthographicChanged.AddListener(SetOrthographic);
        }

        protected override void UnregisterEventListeners()
        {
            base.UnregisterEventListeners();
            var cameraPropertyData = LayerData.GetProperty<CameraPropertyData>();
            cameraPropertyData.OnOrthographicChanged.RemoveListener(SetOrthographic);
            
            layerTool.onOpen.RemoveListener(EnableGhost);
            layerTool.onClose.RemoveListener(DisableGhost);
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
            var cameraPropertyData = LayerData.GetProperty<CameraPropertyData>();
            Camera.main.GetComponent<MoveCameraToCoordinate>().LoadCameraData(cameraPropertyData);
        }
        
        public override void OnSelect(LayerData layer)
        {
            //do not call base to not set transform handles
            foreach (var renderer in GetComponentsInChildren<MeshRenderer>())
            {
                renderer.material.color = selectedColor;
            }
        }

        public override void OnDeselect(LayerData layer)
        {
            base.OnDeselect(layer);
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