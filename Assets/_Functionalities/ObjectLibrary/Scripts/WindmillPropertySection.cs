using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Functionalities.ObjectLibrary
{
    [PropertySection(typeof(WindmillPropertyData))]
    public class WindmillPropertySection : PropertySection
    {
        [SerializeField] private Slider axisHeightSlider;
        [SerializeField] private Slider rotorDiameterSlider;

        private WindmillPropertyData propertyData;

        public override void LoadProperties(List<LayerPropertyData> properties)
        {
            propertyData = properties.FirstOrDefault(p => p is WindmillPropertyData) as WindmillPropertyData;
            axisHeightSlider.value = propertyData.AxisHeight;
            rotorDiameterSlider.value = propertyData.RotorDiameter;
        }

        private void OnEnable()
        {
            axisHeightSlider.onValueChanged.AddListener(HandleAxisHeightChange);
            rotorDiameterSlider.onValueChanged.AddListener(HandleRotorDiameterChange);
        }

        private void OnDisable()
        {
            axisHeightSlider.onValueChanged.RemoveListener(HandleAxisHeightChange);
            rotorDiameterSlider.onValueChanged.RemoveListener(HandleRotorDiameterChange);
        }

        private void HandleAxisHeightChange(float newValue)
        {
            propertyData.AxisHeight = newValue;
        }

        private void HandleRotorDiameterChange(float newValue)
        {
            propertyData.RotorDiameter = newValue;
        }
    }
}