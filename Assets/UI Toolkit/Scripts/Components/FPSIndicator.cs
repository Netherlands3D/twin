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
                UpdateIndicatorColor(value);
            }
        }

        private Color indicatorColor;
        private VisualElement colorIndicator;
        private const int ReducedFPSThreshold = 30;
        private const int GoodFPSThreshold = 50;

        private const string GoodClass = "fps-indicator__color--good";
        private const string ReducedClass = "fps-indicator__color--reduced";
        private const string LowClass = "fps-indicator__color--low";
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

        private void UpdateIndicatorColor(int fps)
        {
            ColorIndicator.EnableInClassList(
                GoodClass,
                fps >= GoodFPSThreshold);

            ColorIndicator.EnableInClassList(
                ReducedClass,
                fps >= ReducedFPSThreshold && fps < GoodFPSThreshold);

            ColorIndicator.EnableInClassList(
                LowClass,
                fps < ReducedFPSThreshold);
        }

        public FPSIndicator()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
        }
    }
}