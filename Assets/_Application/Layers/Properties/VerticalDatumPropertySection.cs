using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class VerticalDatumPropertySection : PropertySectionWithLayerGameObject
    {
        [SerializeField] private Toggle ellipsoidToggle;
        [SerializeField] private Toggle geoidToggle;

        private ILayerPropertyDataWithCRS propertyData;
        
        private void Start()
        {
            propertyData = LayerGameObject
                .LayerData
                .LayerProperties
                .FindAll<ILayerPropertyDataWithCRS>().FirstOrDefault();
            if (propertyData == null) return;

            // Set initial toggle states
            var usesEllipsoid = propertyData.ContentCRS == (int)CoordinateSystem.WGS84_ECEF;
            ellipsoidToggle.isOn = usesEllipsoid;
            geoidToggle.isOn = !usesEllipsoid;
            if (usesEllipsoid)
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

            propertyData.ContentCRS = (int)CoordinateSystem.WGS84_ECEF;
        }
        
        private void SetGeoidHeight(bool isOn)
        {
            if (!isOn)
                return;

            propertyData.ContentCRS = (int)CoordinateSystem.WGS84_NAP_ECEF;
        }
    }
}
