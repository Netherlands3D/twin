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
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private ColorWheel colorPicker;
        public UnityEvent<Color> PickedColor = new();

        private StylingPropertyData stylingPropertyData;


        public void LoadProperties(List<LayerPropertyData> properties)
        {
            stylingPropertyData = properties.Get<StylingPropertyData>();
            if (stylingPropertyData == null) return;
            
            stylingPropertyData.OnStylingApplied.AddListener(UpdateColorFromLayer);
            UpdateColorFromLayer();
        }

        private void Awake()
        {
            PickedColor.AddListener(OnColorPicked);
        }

        private void OnDestroy()
        {
            PickedColor.RemoveListener(OnColorPicked);
            stylingPropertyData.OnStylingApplied.RemoveListener(UpdateColorFromLayer);
        }

        private void OnEnable()
        {
            colorPicker.colorChanged.AddListener(OnPickedColor);
        }

        private void OnDisable()
        {
            colorPicker.colorChanged.RemoveListener(OnPickedColor);
        }

        public void PickColorWithoutNotify(Color color)
        {
            colorPicker.SetColorWithoutNotify(color);
        }      

        public void OnPickedColor(Color color)
        {
            PickedColor.Invoke(color);
            stylingPropertyData.OnStylingApplied.Invoke();
        }

        private void UpdateColorFromLayer()
        {
            Color? color = stylingPropertyData.DefaultStyle.AnyFeature.Symbolizer.GetFillColor();
            this.PickColorWithoutNotify(color.HasValue ? color.Value : defaultColor);
        }

        /// <summary>
        /// Default behaviour is to set the picked color as the, unconditional, fill color for the given layer. This
        /// should suffice in most situations, but if a different behaviour is desired you can remove all listeners
        /// from the PickedColor event and provide your own implementation.
        /// </summary>
        /// <param name="color"></param>
        private void OnColorPicked(Color color)
        {
            stylingPropertyData.DefaultStyle.AnyFeature.Symbolizer.SetFillColor(color);            
        }

    }
}