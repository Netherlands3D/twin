using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class Footer : VisualElement
    {
        // Elements from UXML (cached via Q when accessed)
        private VisualElement divider;
        public VisualElement Divider => divider ??= this.Q<VisualElement>("Divider");

        private Label appName;
        public Label AppName => appName ??= this.Q<Label>("AppName");

        private Label attribution;
        public Label Attribution => attribution ??= this.Q<Label>("Attribution");

        private Label coordinateX;
        public Label CoordinateX => coordinateX ??= this.Q<Label>("CoordinateX");

        private Label coordinateY;
        public Label CoordinateY => coordinateY ??= this.Q<Label>("CoordinateY");

        private Label coordinateZ;
        public Label CoordinateZ => coordinateZ ??= this.Q<Label>("CoordinateZ");

        public Footer()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
        }
    }
}