using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ToolbarMain : VisualElement
    {
        public Button LayerButton => this.Q<Button>("Layer");
        public Button LibraryButton => this.Q<Button>("Library");
        public Button AddButton => this.Q<Button>("Add");
        public Button SearchButton => this.Q<Button>("Search");
        public Button SunPositionButton => this.Q<Button>("SunPosition");
        public Button DownloadTileButton => this.Q<Button>("DownloadTile");

        private VisualElement divider;
        public VisualElement Divider => divider ??= this.Q<VisualElement>("Divider");

        public ToolbarMain()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            // TODO: Register events
        }
    }
}