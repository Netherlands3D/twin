using System.Collections.Generic;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI_Toolkit.Scripts;
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
        private ColorTile colorTile;
        
        public UnityEvent<Color> ColorSelected = new();
        
        public ColorPicker()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");    
            
            colorSpectrum = this.Q<ColorSpectrum>();
            brightnessSlider = this.Q<ColorSlider>();
            hexField = this.Q<TextField>();
            colorTile = this.Q<ColorTile>();

            hexField.RegisterValueChangedCallback(OnHexValueChanged);
            hexField.RegisterCallback<NavigationSubmitEvent>(_ => OnHexValueChanged(null), TrickleDown.TrickleDown);
            
            colorSpectrum.SpectrumChanged.AddListener(OnColorInputChanged);
            brightnessSlider.RegisterValueChangedCallback(_ => OnColorInputChanged());
            
            ColorSelected.AddListener(SetColorTileColor);
        }

        private void SetColorTileColor(Color newColor)
        {
            string hex = ColorUtility.ToHtmlStringRGB(newColor);
            colorTile.Color = hex;
        }

        private void OnHexValueChanged(ChangeEvent<string> evt)
        {
            if(!hexField.hasFocusPseudoState) //don't trigger an infinite loop if the text is updated through the Spectrum or slider changing
                return;
            
            if (!HexColorUtility.ParseHexColor(hexField.text, out var color)) return;

            SetColorInputComponents(color);
        }

        private void SetColorInputComponents(Color newColor)
        {
            Color.RGBToHSV(newColor, out float h, out float s, out float v);
            colorSpectrum.SetValueWithoutNotify(h * 360f, s);
            brightnessSlider.SetValueWithoutNotify(v);
            SetInputComponentsColorTint(colorSpectrum.Hue, colorSpectrum.Saturation, 1);
            ColorSelected.Invoke(newColor);
        }

        private void SetInputComponentsColorTint(float h, float s, float v)
        {
            var newColorFullBrightness = ColorUtility.ToHtmlStringRGB(Color.HSVToRGB(h/360, s, 1));
            Debug.Log(newColorFullBrightness);
            brightnessSlider.Color = newColorFullBrightness;
            
            // colorSpectrum.tint
        }

        private void OnColorInputChanged()
        {
            Debug.Log(colorSpectrum.Hue + " " + colorSpectrum.Saturation + " " + brightnessSlider.value);
            var newColor = Color.HSVToRGB(colorSpectrum.Hue / 360f, colorSpectrum.Saturation, brightnessSlider.value);
            Debug.Log(newColor);
            string hex = ColorUtility.ToHtmlStringRGB(newColor);
            hexField.SetText($"#{hex}");
            SetInputComponentsColorTint(colorSpectrum.Hue, colorSpectrum.Saturation, 1);
            ColorSelected.Invoke(newColor);
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            
        }
    }
}