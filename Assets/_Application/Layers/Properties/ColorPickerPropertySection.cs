using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.UI.ColorPicker;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [PropertySection(typeof(StylingPropertyData), Symbolizer.FillColorProperty)]
    public class ColorPickerPropertySection : MonoBehaviour, IVisualizationWithPropertyData
    {
        public ColorWheel ColorWheel => colorWheel;

        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private ColorWheel colorWheel;

        private StylingPropertyData stylingPropertyData;

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            stylingPropertyData = properties.Get<StylingPropertyData>();
            if (stylingPropertyData == null) return;
            
            stylingPropertyData.OnStylingApplied.AddListener(UpdateColorFromProperty);
            colorWheel.colorChanged.AddListener(OnColorPicked);

            UpdateColorFromProperty();
        }

        private void OnDestroy()
        {       
            stylingPropertyData?.OnStylingApplied.RemoveListener(UpdateColorFromProperty);
            colorWheel.colorChanged.RemoveListener(OnColorPicked);
        }

        public void PickColorWithoutNotify(Color color)
        {
            colorWheel.SetColorWithoutNotify(color);
        }   

        private void UpdateColorFromProperty()
        {
            Color? color = stylingPropertyData.DefaultStyle.AnyFeature.Symbolizer.GetFillColor();
            colorWheel.SetColorWithoutNotify(color.HasValue ? color.Value : defaultColor);
        }

        private void OnColorPicked(Color color)
        {
            stylingPropertyData.DefaultStyle.AnyFeature.Symbolizer.SetFillColor(color);
            stylingPropertyData.OnStylingApplied.Invoke();
        }
    }
}