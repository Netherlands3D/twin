using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using TextField = Netherlands3D.UI.Components.TextField;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class ColorPicker : VisualElement
    {
        private ColorSpectrum colorSpectrum;
        private ColorSlider colorSlider;
        private TextField hexField;
        
        public UnityEvent<Color> OnColorSelected;
        
        public ColorPicker()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");    
            
            colorSpectrum = this.Q<ColorSpectrum>();
            colorSlider = this.Q<ColorSlider>();
            hexField = this.Q<TextField>();
            
        }
        
        
        
    }
}