using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ToolbarMain : VisualElement
    {
        public ToggleButtonGroup Group => this.Q<ToggleButtonGroup>("ButtonGroup");
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

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                // Defaults: single selection, empty selection allowed
                Group.allowEmptySelection = true;
                Group.isMultipleSelection = false;

                // Clear selection: bitmask 0, length = number of options
                int optionCount = Group.childCount;
                Group.SetValueWithoutNotify(new ToggleButtonGroupState(0ul, optionCount));
            });
        }
    }
}