using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ToolbarProperties : VisualElement
    {
        public ToggleButtonGroup Group => this.Q<ToggleButtonGroup>("ButtonGroup");
        public Button Information => this.Q<Button>("Information");
        public Button Properties => this.Q<Button>("Properties");
        public Button Styles => this.Q<Button>("Styles");

        public EventCallback<ChangeEvent<bool>> OnOpenInformationToggled { get; set; }
        public EventCallback<ChangeEvent<bool>> OnPropertiesToggled { get; set; }
        public EventCallback<ChangeEvent<bool>> OnStylesToggled { get; set; }

        public ToolbarProperties()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            // Register exposed events

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                // Defaults: single selection, empty selection allowed
                Group.allowEmptySelection = true;
                Group.isMultipleSelection = false;
            });
        }
    }
}
