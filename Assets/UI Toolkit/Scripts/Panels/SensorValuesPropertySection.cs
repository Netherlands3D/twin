using System.Collections.Generic;
using Netherlands3D.Functionalities.UrbanReLeaf;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Slider = Netherlands3D.UI.Components.Slider;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(SensorPropertyData))]
    public partial class SensorValuesPropertySection : VisualElement, IVisualizationWithPropertyData
    {
        private SensorPropertyData propertyData;

        private Slider minimumValueSlider;
        private Slider maximumValueSlider;
        private ColorTile minimumColorTile;
        private ColorTile maximumColorTile;

        private Button resetButton;

        public SensorValuesPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            minimumValueSlider = this.Q<Slider>("MinimumValueSlider");
            maximumValueSlider = this.Q<Slider>("MaximumValueSlider");
            minimumColorTile = this.Q<ColorTile>("MinimumColorTile");
            maximumColorTile = this.Q<ColorTile>("MaximumColorTile");

            minimumValueSlider.RegisterValueChangedCallback(OnMinimumValueChanged);
            maximumValueSlider.RegisterValueChangedCallback(OnMaximumValueChanged);

            resetButton = this.Q<Button>();
            resetButton.RegisterCallback<ClickEvent>(OnResetButtonClicked);
        }

        private void OnMinimumValueChanged(ChangeEvent<float> evt)
        {
            propertyData.MinValue = evt.newValue;
        }
        
        private void OnMaximumValueChanged(ChangeEvent<float> evt)
        {
            propertyData.MaxValue = evt.newValue;
        }

        private void OnResetButtonClicked(ClickEvent evt)
        {
            propertyData.ResetMinMaxValues();
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            propertyData = properties.Get<SensorPropertyData>();

            propertyData.OnMinValueChanged.AddListener(UpdateMinimumSlider);
            propertyData.OnMaxValueChanged.AddListener(UpdateMaximumSlider);
            propertyData.OnMinColorChanged.AddListener(UpdateMinimumColor);
            propertyData.OnMaxColorChanged.AddListener(UpdateMaximumColor);

            UpdateMinimumSlider(propertyData.MinValue);
            UpdateMaximumSlider(propertyData.MaxValue);
            UpdateMinimumColor(propertyData.MinColor);
            UpdateMaximumColor(propertyData.MaxColor);
        }

        private void UpdateMinimumSlider(float value)
        {
            minimumValueSlider.SetValueWithoutNotify(value);
        }

        private void UpdateMaximumSlider(float value)
        {
            maximumValueSlider.SetValueWithoutNotify(value);
        }

        private void UpdateMinimumColor(Color color)
        {
            minimumColorTile.Color = color;
        }

        private void UpdateMaximumColor(Color color)
        {
            maximumColorTile.Color = color;
        }
    }
}