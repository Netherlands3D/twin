using System;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ToolbarMain : VisualElement
    {
        public ToggleButtonGroup Group => this.Q<ToggleButtonGroup>("ButtonGroup");

        public ToolbarMain()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            RegisterCallback<AttachToPanelEvent>(NotifyAttachedToPanel);
        }

        private void NotifyAttachedToPanel(AttachToPanelEvent _)
        {
            Group.RegisterValueChangedCallback(NotifyValueChanged);
            ClearWithoutNotify();
        }

        private void NotifyValueChanged(ChangeEvent<ToggleButtonGroupState> evt)
        {
            var newValue = evt.newValue.GetActiveOptions(stackalloc int[Group.value.length]);
            ToolType type = newValue.Length > 0 ? (ToolType)newValue[0] : ToolType.None;
            Services.ServiceLocator.GetService<ToolService>().GetTool(type)?.OpenInspector();
        }

        public void ClearWithoutNotify()
        {
            Group.SetValueWithoutNotify(new ToggleButtonGroupState(0ul, Group.value.length));
        }

        public void EnableToolWithoutNotify(ToolType tool)
        {
            var bits = 1ul << (int)tool;
            Group.SetValueWithoutNotify(new ToggleButtonGroupState(bits, Group.value.length));
        }
    }
}