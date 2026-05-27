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
    [PropertySection(typeof(SensorPropertyData), PropertySectionCategory.Styling)]
    public partial class SensorValuesPropertySection : VisualElement, IVisualizationWithPropertyData, IPropertyPanelWithColorPicker
    {
        private SensorPropertyData propertyData;

        private Slider minimumValueSlider;
        private Slider maximumValueSlider;
        private ColorTile minimumColorTile;
        private ColorTile maximumColorTile;

        private ColorTile activeTile;

        private Button resetButton;
        
        public ColorPicker ColorPicker { get; set; }
        private ColorPickerState colorPickerState = ColorPickerState.None;
        
        private enum ColorPickerState
        {
            None,
            Maximum,
            Minimum
        }

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
            minimumColorTile.RegisterCallback<ClickEvent>(OnMinimumColorTileClicked);
            maximumColorTile.RegisterCallback<ClickEvent>(OnMaximumColorTileClicked);

            resetButton = this.Q<Button>();
            resetButton.RegisterCallback<ClickEvent>(OnResetButtonClicked);
            
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            SetColorPickerState(ColorPickerState.None);
            ColorPicker.ColorChanged.RemoveListener(OnColorPicked);
        }

        private void OnMinimumColorTileClicked(ClickEvent evt)
        {
            activeTile = minimumColorTile;

            if (colorPickerState == ColorPickerState.Minimum)
            {
                SetColorPickerState(ColorPickerState.None);
                return;
            }
            
            SetColorPickerState(ColorPickerState.Minimum);
            ColorPicker.SetColorInputComponentsWithoutNotify(minimumColorTile.Color);
        }

        private void OnMaximumColorTileClicked(ClickEvent evt)
        {
            activeTile = maximumColorTile;
            
            if (colorPickerState == ColorPickerState.Maximum)
            {
                SetColorPickerState(ColorPickerState.None);
                return;
            }
            
            SetColorPickerState(ColorPickerState.Maximum);
            ColorPicker.SetColorInputComponentsWithoutNotify(maximumColorTile.Color);
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
            
            ColorPicker.ColorChanged.AddListener(OnColorPicked);
        }

        private void OnColorPicked(Color newColor)
        {
            if(activeTile == minimumColorTile)
                propertyData.MinColor = newColor;
            else if(activeTile == maximumColorTile)
                propertyData.MaxColor = newColor;
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

        private void SetColorPickerState(ColorPickerState newState)
        {
            ColorPicker.SetVisible(newState != ColorPickerState.None);
            colorPickerState = newState;
        }
    }
}