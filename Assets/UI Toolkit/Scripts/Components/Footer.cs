using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class Footer : VisualElement
    {
        private Label attributionLabel;
        private Label AttributionLabel => attributionLabel ??= this.Q<Label>("Attribution");

        private CoordinateLabel coordinateLabel;
        private CoordinateLabel CoordinateLabel => coordinateLabel ??= this.Q<CoordinateLabel>();

        [UxmlAttribute("attribution")]
        public string Attribution
        {
            get => AttributionLabel.text;
            set => AttributionLabel.text = value;
        }

        [UxmlAttribute("x")]
        public float X
        {
            get => CoordinateLabel.X;
            set => CoordinateLabel.X = value;
        }

        [UxmlAttribute("y")]
        public float Y
        {
            get => CoordinateLabel.Y;
            set => CoordinateLabel.Y = value;
        }

        [UxmlAttribute("z")]
        public float Z
        {
            get => CoordinateLabel.Z;
            set => CoordinateLabel.Z = value;
        }

        public Footer()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
        }
    }
}