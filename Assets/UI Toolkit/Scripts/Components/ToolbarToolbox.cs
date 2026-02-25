using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ToolbarToolbox : VisualElement
    {
        public Toggle LayerButton => this.Q<Toggle>("Screenshot");
        public Toggle LibraryButton => this.Q<Toggle>("Dome");

        public ToolbarToolbox()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            // TODO: Register events
        }
    }
}