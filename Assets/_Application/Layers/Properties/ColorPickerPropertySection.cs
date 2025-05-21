using Netherlands3D.Twin.UI.ColorPicker;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class ColorPickerPropertySection : PropertySectionWithLayerGameObject
    {
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private ColorWheel colorPicker;
        private LayerGameObject layer;

        public override LayerGameObject LayerGameObject
        {
            get => layer;
            set
            {
                layer = value;
                colorPicker.SetColorWithoutNotify(layer.LayerData.DefaultStyle.AnyFeature.Symbolizer.GetFillColor() ?? defaultColor);
            }
        }

        public void SetColorPickerColor(Color color)
        {
            colorPicker.SetColorWithoutNotify(color);
        }

        private void OnEnable()
        {
            colorPicker.colorChanged.AddListener(OnPickedColor);
        }

        private void OnDisable()
        {
            colorPicker.colorChanged.RemoveListener(OnPickedColor);
        }

        public void OnPickedColor(Color color)
        {
            layer.LayerData.DefaultStyle.AnyFeature.Symbolizer.SetFillColor(color);
            layer.ApplyStyling();
        }
    }
}