using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ToolbarToolbox : VisualElement
    {
        public ToggleButtonGroup Group => this.Q<ToggleButtonGroup>("ButtonGroup");
        public Button Dome => this.Q<Button>("Screenshot");
        public Button Screenshot => this.Q<Button>("Dome");
        public ToolbarToolbox()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

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