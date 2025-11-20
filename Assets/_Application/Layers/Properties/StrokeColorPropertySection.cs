using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.UI.ColorPicker;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class StrokeColorPropertySection : MonoBehaviour, IVisualizationWithPropertyData
    {
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private ColorWheel colorPicker;

        private StylingPropertyData stylingPropertyData;    

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            stylingPropertyData = properties.Get<StylingPropertyData>();
            if (stylingPropertyData == null) return;

            colorPicker.SetColorWithoutNotify(stylingPropertyData.DefaultStyle.AnyFeature.Symbolizer.GetFillColor() ?? defaultColor);
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
            stylingPropertyData.DefaultStyle.AnyFeature.Symbolizer.SetStrokeColor(color);
            stylingPropertyData.OnStylingApplied.Invoke();
        }
    }
}