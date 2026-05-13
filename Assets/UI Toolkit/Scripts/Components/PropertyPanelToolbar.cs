using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class PropertyPanelToolbar : VisualElement
    {
        public ToggleButtonGroup Group => this.Q<ToggleButtonGroup>("ButtonGroup");
        public Button Information => this.Q<Button>("Information");
        public Button Settings => this.Q<Button>("Settings");
        public Button Styles => this.Q<Button>("Styles");
        public PropertySectionCategory State => (PropertySectionCategory)Group.value.GetActiveOptions(new int[Group.value.length])[0];

        public PropertyPanelToolbar()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
        }

        public void UpdateState()
        {
            var state = new ToggleButtonGroupState(0, Group.value.length);
            int firstActive = 0;
            for (int i = 0; i < state.length; i++)
            {
                var button = Group.GetButton(i);
                if (button.hasEnabledPseudoState)
                {
                    firstActive = i;
                    break;
                }
            }
            state[firstActive] = true;
            Group.value = state;
        }
    }
}
