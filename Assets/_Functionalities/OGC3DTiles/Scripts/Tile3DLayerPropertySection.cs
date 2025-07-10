using System;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Functionalities.OGC3DTiles
{
    public class Tile3DLayerPropertySection : PropertySectionWithLayerGameObject
    {
        [SerializeField] private Toggle ellipsoidToggle;
        [SerializeField] private Toggle geoidToggle;

        private void OnEnable()
        {
            ellipsoidToggle.onValueChanged.AddListener(SetEllipsoidHeight);
            geoidToggle.onValueChanged.AddListener(SetGeoidHeight);
        }

        private void OnDisable()
        {
            ellipsoidToggle.onValueChanged.RemoveListener(SetEllipsoidHeight);
            geoidToggle.onValueChanged.RemoveListener(SetGeoidHeight);
        }

        public void SetEllipsoidHeight(bool isOn)
        {
            if (!isOn)
                return;
            var tile3dLayerGameObject = LayerGameObject as Tile3DLayerGameObject;
            tile3dLayerGameObject.PropertyData.ContentCRS = (int)CoordinateSystem.WGS84_ECEF;
        }
        
        public void SetGeoidHeight(bool isOn)
        {
            if (!isOn)
                return;
            var tile3dLayerGameObject = LayerGameObject as Tile3DLayerGameObject;
            tile3dLayerGameObject.PropertyData.ContentCRS = (int)CoordinateSystem.WGS84_NAP_ECEF;
        }
    }
}
