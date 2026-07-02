using UnityEngine;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class FPSIndicator : VisualElement
    {
        private int fpsValue;
        private Label fpsValueLabel;
        private Label FPSValueLabel =>
            fpsValueLabel ??= this.Q<Label>("FPSValueLabel");


        [UxmlAttribute("fps-value")]
        public int FPSValue
        {
            get => fpsValue;
            set
            {
                fpsValue = value;
                FPSValueLabel.text = $"{value} FPS";
            }
        }

        private Color indicatorColor;
        private VisualElement colorIndicator;
        private VisualElement ColorIndicator =>
            colorIndicator ??= this.Q<VisualElement>("ColorIndicator");

        [UxmlAttribute("indicator-color")]
        public Color IndicatorColor
        {
            get => indicatorColor;
            set
            {
                indicatorColor = value;
                ColorIndicator.style.backgroundColor = value;
            }
        }

        public FPSIndicator()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
        }
    }
}