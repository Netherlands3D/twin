using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.UI.ColorPicker;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class ColorPickerPropertySection : MonoBehaviour
    {
        public UnityEvent<StylingRule> ColorChanged = new();
        public StylingRule selectedStylingRule;

        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private ColorWheel colorPicker;

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
            selectedStylingRule.Symbolizer.SetFillColor(color);
            ColorChanged.Invoke(selectedStylingRule);
        }
    }
}