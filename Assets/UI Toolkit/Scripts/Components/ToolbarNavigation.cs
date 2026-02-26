using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ToolbarNavigation : VisualElement
    {
        public ToggleButtonGroup Group => this.Q<ToggleButtonGroup>("ButtonGroup");
        public Button FPV => this.Q<Button>("FPV");
        public Button North => this.Q<Button>("North");
        public Button Perspective => this.Q<Button>("Perspective");

        public ToolbarNavigation()
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