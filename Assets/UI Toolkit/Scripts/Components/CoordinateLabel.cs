using System.Globalization;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class CoordinateLabel : VisualElement
    {
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
            set => CoordinateXLabel.text = value.ToString("F0", CultureInfo.InvariantCulture);
        }

        [UxmlAttribute("y")]
        public float Y
        {
            get => float.Parse(CoordinateYLabel.text);
            set => CoordinateYLabel.text = value.ToString("F0", CultureInfo.InvariantCulture);
        }

        [UxmlAttribute("z")]
        public float Z
        {
            get => float.Parse(CoordinateZLabel.text);
            set => CoordinateZLabel.text = value.ToString("F0", CultureInfo.InvariantCulture);
        }

        public CoordinateLabel()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
        }
    }
}