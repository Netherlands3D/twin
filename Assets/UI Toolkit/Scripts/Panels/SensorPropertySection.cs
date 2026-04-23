using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Netherlands3D.Functionalities.ObjectLibrary;
using Netherlands3D.Functionalities.UrbanReLeaf;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;
using Slider = UnityEngine.UIElements.Slider;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(SensorPropertyData))]
    public partial class SensorPropertySection : VisualElement, IVisualizationWithPropertyData
    {
        private SensorPropertyData propertyData;
        private Slider axisHeightSlider;
        private Slider rotorDiameterSlider;

        private XYZField position;
        
        public SensorPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");    
            
            // axisHeightSlider = this.Q<Slider>("Ashoogte");
            // rotorDiameterSlider = this.Q<Slider>("Rotordiameter");
            //
            // axisHeightSlider.RegisterValueChangedCallback(HandleAxisHeightChange);
            // rotorDiameterSlider.RegisterValueChangedCallback(HandleRotorDiameterChange);
        }


        // private void HandleAxisHeightChange(ChangeEvent<float> evt)
        // {
        //     propertyData.AxisHeight = evt.newValue;
        // }
        //
        // private void HandleRotorDiameterChange(ChangeEvent<float> evt)
        // {
        //     propertyData.RotorDiameter = evt.newValue;
        // }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            propertyData = properties.Get<SensorPropertyData>();

            propertyData.OnMinValueChanged.AddListener(UpdateMinimumSlider);
            propertyData.OnMaxValueChanged.AddListener(UpdateMaximumSlider);
            propertyData.OnMinColorChanged.AddListener(UpdateMinimumColor);
            propertyData.OnMaxColorChanged.AddListener(UpdateMaximumColor);
            propertyData.OnStartDateChanged.AddListener(UpdateStartDate);
            propertyData.OnEndDateChanged.AddListener(UpdateEndDate);
            
            UpdateStartDate(propertyData.StartDate);
            UpdateEndDate(propertyData.EndDate);
            UpdateMinimumSlider(propertyData.MinValue);
            UpdateMaximumSlider(propertyData.MaxValue);
            UpdateMinimumColor(propertyData.MinColor);
            UpdateMaximumColor(propertyData.MaxColor);
        }
        
        private void UpdateStartDate(DateTime startDate)
        {
            // startTimeYearField.text = startDate.Year.ToString();
            // startTimeMonthField.text = startDate.Month.ToString();
            // startTimeDayField.text = startDate.Day.ToString();
            //
            // startTimeYearInputField.text = startTimeYearField.text;
            // startTimeMonthInputField.text = startTimeMonthField.text;
            // startTimeDayInputField.text = startTimeDayField.text;
        }

        private void UpdateEndDate(DateTime endDate)
        {
            // endTimeYearField.text = endDate.Year.ToString();
            // endTimeMonthField.text = endDate.Month.ToString();
            // endTimeDayField.text = endDate.Day.ToString();
            //
            // endTimeYearInputField.text = endTimeYearField.text;
            // endTimeMonthInputField.text = endTimeMonthField.text;
            // endTimeDayInputField.text = endTimeDayField.text;
        }
        
        private void UpdateMinimumSlider(float value)
        {
                // minSlider.value = value;
        }
        
        private void UpdateMaximumSlider(float value)
        {
                // maxSlider.value = value;
        }

        private void UpdateMinimumColor(Color color)
        {
                // minimumColorPicker.color = color;
        }

        private void UpdateMaximumColor(Color color)
        {
                // maximumColorPicker.color = color;
        }

    }
}