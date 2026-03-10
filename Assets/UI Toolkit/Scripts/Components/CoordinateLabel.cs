using System.Globalization;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class CoordinateLabel : VisualElement
    {
        /// <summary>
        /// Force culture to Dutch - the main group is dutch and otherwise confusion may happen since
        /// coordinates are comma-separated and in en-US and similar cultures the thousand separator is
        /// also a comma.
        /// </summary>
        private readonly CultureInfo culture = CultureInfo.GetCultureInfoByIetfLanguageTag("nl-NL");
        
        private Label coordinateXLabel;
        private Label CoordinateXLabel => coordinateXLabel ??= this.Q<Label>("CoordinateX");

        private Label coordinateYLabel;
        private Label CoordinateYLabel => coordinateYLabel ??= this.Q<Label>("CoordinateY");

        private Label coordinateZLabel;
        private Label CoordinateZLabel => coordinateZLabel ??= this.Q<Label>("CoordinateZ");

        [UxmlAttribute("x")]
        public float X
        {
            get => float.Parse(CoordinateXLabel.text);
            set => CoordinateXLabel.text = value.ToString("N0", culture);
        }

        [UxmlAttribute("y")]
        public float Y
        {
            get => float.Parse(CoordinateYLabel.text);
            set => CoordinateYLabel.text = value.ToString("N0", culture);
        }

        [UxmlAttribute("z")]
        public float Z
        {
            get => float.Parse(CoordinateZLabel.text);
            set => CoordinateZLabel.text = value.ToString("N0", culture);
        }

        public CoordinateLabel()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
        }
    }
}