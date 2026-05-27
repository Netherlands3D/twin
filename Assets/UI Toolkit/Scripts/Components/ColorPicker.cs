using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ColorPicker : VisualElement
    {
        private ColorSpectrum colorSpectrum;
        private ColorSlider brightnessSlider;
        private TextField hexField;
        private ColorTile colorTile;

        public UnityEvent<Color> ColorChanged = new();
        public UnityEvent<bool> ColorPickerVisibilityChanged = new();

        public ColorPicker()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            colorSpectrum = this.Q<ColorSpectrum>();
            brightnessSlider = this.Q<ColorSlider>();
            hexField = this.Q<TextField>();
            colorTile = this.Q<ColorTile>();

            hexField.RegisterValueChangedCallback(OnHexValueChanged);
            hexField.RegisterCallback<NavigationSubmitEvent>(OnHexColorSubmitted, TrickleDown.TrickleDown);

            colorSpectrum.SpectrumChanged.AddListener(OnColorInputChanged);
            brightnessSlider.RegisterValueChangedCallback(_ => OnColorInputChanged());
        }

        private void OnHexColorSubmitted(NavigationSubmitEvent evt)
        {
            OnHexValueChanged(null);
            //reset or format hex text
            var newColor = Color.HSVToRGB(colorSpectrum.Hue / 360f, colorSpectrum.Saturation, brightnessSlider.value / 255f);
            string hex = ColorUtility.ToHtmlStringRGB(newColor);
            hexField.SetText($"#{hex}");
        }

        private void OnHexValueChanged(ChangeEvent<string> evt)
        {
            if (!hexField.hasFocusPseudoState) //don't trigger an infinite loop if the text is updated through the Spectrum or slider changing
                return;

            if (!HexColorUtility.ParseHexColor(hexField.text, out var color)) return;
            
            SetColorInputComponentsWithoutNotify(color);
            ColorChanged.Invoke(color);
        }

        public void SetColorInputComponentsWithoutNotify(Color newColor)
        {
            Color.RGBToHSV(newColor, out float h, out float s, out float v);
            colorSpectrum.SetValueWithoutNotify(h * 360f, s);
            brightnessSlider.SetValueWithoutNotify(v * 255f);
            SetInputComponentsColorTint(colorSpectrum.Hue, colorSpectrum.Saturation, brightnessSlider.value / 255f);
            string hex = ColorUtility.ToHtmlStringRGB(newColor);
            hexField.SetText($"#{hex}");
            colorTile.Color = newColor;
        }

        private void SetInputComponentsColorTint(float h, float s, float v)
        {
            var newColorFullBrightness = ColorUtility.ToHtmlStringRGB(Color.HSVToRGB(h / 360, s, 1));
            brightnessSlider.Color = newColorFullBrightness;
            colorSpectrum.Brightness = v;
        }

        private void OnColorInputChanged()
        {
            var newColor = Color.HSVToRGB(colorSpectrum.Hue / 360f, colorSpectrum.Saturation, brightnessSlider.value / 255f);
            SetColorInputComponentsWithoutNotify(newColor);
            ColorChanged.Invoke(newColor);
        }

        public void SetVisible(bool visible)
        {
            EnableInClassList(UtilityClassConstants.HIDDEN, !visible);
            ColorPickerVisibilityChanged.Invoke(visible);
        }
    }
}