using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers;
using UnityEngine;

namespace Netherlands3D.Functionalities.ObjectLibrary
{
    public class FillColorPropertySection : MonoBehaviour
    {
        private LayerData layer;
        
        [SerializeField] private Color defaultColor = Color.gray;
        [SerializeField] private GameObject colorPicker;
        
        public LayerData Layer
        {
            get => layer;
            set
            {
                layer = value;
                // TODO: Set ColorPicker's selected color value
                // colorPicker.SelectedColor = layer.DefaultStyle.Default.Symbolizer.GetFillColor() ?? defaultColor;
            }
        }

        private void OnEnable()
        {
            // TODO: register listener on colorpicker to change the color: OnPickedColor
        }
        
        private void OnDisable()
        {
            // TODO: unregister listener on colorpicker to change the color: OnPickedColor
        }

        private void OnPickedColor(Color color)
        {
            layer.DefaultStyle.Default.Symbolizer.SetFillColor(color);
        }
    }
}