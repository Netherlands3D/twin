using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ToolbarNavigation : VisualElement
    {
        public Toggle FPV => this.Q<Toggle>("FPV");
        public Toggle North => this.Q<Toggle>("North");
        public Toggle Perspective => this.Q<Toggle>("Perspective");

        public ToolbarNavigation()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            // TODO: Register events
        }
    }
}