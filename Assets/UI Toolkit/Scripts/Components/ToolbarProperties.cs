using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ToolbarProperties : VisualElement
    {
        public Button Information => this.Q<Button>("Information");
        public Button Settings => this.Q<Button>("Settings");
        public Button Styles => this.Q<Button>("Styles");

        public ToolbarProperties()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
        }
    }
}
