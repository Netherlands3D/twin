using System.Collections.Generic;
using Netherlands3D.Functionalities.ObjectLibrary;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;
using Slider = UnityEngine.UIElements.Slider;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(WindmillPropertyData), PropertySectionCategory.Settings)]
    public partial class WindmillPropertySection : VisualElement, IVisualizationWithPropertyData
    {
        private WindmillPropertyData propertyData;
        private Slider axisHeightSlider;
        private Slider rotorDiameterSlider;
        
        public WindmillPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");    
            
            axisHeightSlider = this.Q<Slider>("Ashoogte");
            rotorDiameterSlider = this.Q<Slider>("Rotordiameter");
            
            axisHeightSlider.RegisterValueChangedCallback(HandleAxisHeightChange);
            rotorDiameterSlider.RegisterValueChangedCallback(HandleRotorDiameterChange);
        }


        private void HandleAxisHeightChange(ChangeEvent<float> evt)
        {
            propertyData.AxisHeight = evt.newValue;
        }

        private void HandleRotorDiameterChange(ChangeEvent<float> evt)
        {
            propertyData.RotorDiameter = evt.newValue;
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            propertyData = properties.Get<WindmillPropertyData>();
            axisHeightSlider.value = propertyData.AxisHeight;
            rotorDiameterSlider.value = propertyData.RotorDiameter;
        }
    }
}