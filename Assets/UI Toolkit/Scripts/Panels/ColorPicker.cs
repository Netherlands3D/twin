using System.Collections.Generic;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using TextField = Netherlands3D.UI.Components.TextField;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(ColorPropertyData))]

    public partial class ColorPicker : VisualElement, IVisualizationWithPropertyData
    {
        private ColorSpectrum colorSpectrum;
        private ColorSlider brightnessSlider;
        private TextField hexField;
        
        public UnityEvent<Color> ColorSelected = new();
        
        public ColorPicker()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");    
            
            colorSpectrum = this.Q<ColorSpectrum>();
            brightnessSlider = this.Q<ColorSlider>();
            hexField = this.Q<TextField>();

            hexField.RegisterValueChangedCallback(OnHexValueChanged);
            hexField.RegisterCallback<NavigationSubmitEvent>(_ => OnHexValueChanged(null), TrickleDown.TrickleDown);
            
            colorSpectrum.SpectrumChanged.AddListener(OnColorInputChanged);
            brightnessSlider.RegisterValueChangedCallback(_ => OnColorInputChanged());
        }

        private void OnHexValueChanged(ChangeEvent<string> evt)
        {
            var hexString = hexField.text;
            if (!hexString.StartsWith("#"))
            {
                hexString = "#" + hexString;
            }

            if (hexString.Length != 7 && hexString.Length != 9)
            {
                Debug.LogWarning("Invalid HEX format. Ensure it is 6 or 8 characters long after '#'.");
                return;
            }

            if (!ColorUtility.TryParseHtmlString(hexString, out Color color))
            {
                Debug.LogWarning("Failed to parse color from hex code: " + hexString);
                return;
            }

            SetColor(color);
        }

        private void SetColor(Color newColor)
        {
            Color.RGBToHSV(newColor, out float h, out float s, out float v);
            colorSpectrum.SetValueWithoutNotify(h, s);
            brightnessSlider.SetValueWithoutNotify(v);
        }

        private void OnColorInputChanged()
        {
            var newColor = Color.HSVToRGB(colorSpectrum.Hue/360f, colorSpectrum.Saturation, brightnessSlider.value);
            UpdateHexColorText(newColor);
            ColorSelected.Invoke(newColor);
        }
        
        private void UpdateHexColorText(Color newColor)
        {
            string hex = ColorUtility.ToHtmlStringRGB(newColor);
            hexField.SetText($"#{hex}");
        }


        public void LoadProperties(List<LayerPropertyData> properties)
        {
            
        }
    }
}