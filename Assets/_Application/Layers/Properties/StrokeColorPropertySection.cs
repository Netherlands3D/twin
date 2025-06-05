using Netherlands3D.Twin.UI.ColorPicker;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class StrokeColorPropertySection : PropertySectionWithLayerGameObject
    {
        private LayerGameObject layer;
        
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private ColorWheel colorPicker;
        
        public override LayerGameObject LayerGameObject
        {
            get => layer;
            set
            {
                layer = value;
                colorPicker.SetColorWithoutNotify(layer.LayerData.DefaultStyle.AnyFeature.Symbolizer.GetStrokeColor() ?? defaultColor);
            }
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
            layer.LayerData.DefaultStyle.AnyFeature.Symbolizer.SetStrokeColor(color);
            layer.ApplyStyling();
        }
    }
}