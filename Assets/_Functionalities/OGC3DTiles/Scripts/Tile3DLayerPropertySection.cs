using System;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Functionalities.OGC3DTiles
{
    public class Tile3DLayerPropertySection : PropertySectionWithLayerGameObject
    {
        [SerializeField] private Toggle ellipsoidToggle;
        [SerializeField] private Toggle geoidToggle;
        
        private void Start()
        {
            var tile3dLayerGameObject = LayerGameObject as Tile3DLayerGameObject;
            var usesEllipsoid = tile3dLayerGameObject.PropertyData.ContentCRS == (int)CoordinateSystem.WGS84_ECEF;

            if(usesEllipsoid)
                ellipsoidToggle.isOn = true;
            else
                geoidToggle.isOn = true;
            
            ellipsoidToggle.onValueChanged.AddListener(SetEllipsoidHeight);
            geoidToggle.onValueChanged.AddListener(SetGeoidHeight);
        }

        private void OnDestroy()
        {
            ellipsoidToggle.onValueChanged.RemoveListener(SetEllipsoidHeight);
            geoidToggle.onValueChanged.RemoveListener(SetGeoidHeight);
        }

        private void SetEllipsoidHeight(bool isOn)
        {
            if (!isOn)
                return;
            var tile3dLayerGameObject = LayerGameObject as Tile3DLayerGameObject;
            tile3dLayerGameObject.PropertyData.ContentCRS = (int)CoordinateSystem.WGS84_ECEF;
        }
        
        private void SetGeoidHeight(bool isOn)
        {
            if (!isOn)
                return;
            var tile3dLayerGameObject = LayerGameObject as Tile3DLayerGameObject;
            tile3dLayerGameObject.PropertyData.ContentCRS = (int)CoordinateSystem.WGS84_NAP_ECEF;
        }
    }
}
