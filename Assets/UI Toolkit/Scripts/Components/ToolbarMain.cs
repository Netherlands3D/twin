using System;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ToolbarMain : VisualElement
    {
        public ToggleButtonGroup Group => this.Q<ToggleButtonGroup>("ButtonGroup");
        private ToolService tools;

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
            tools = Services.ServiceLocator.GetService<ToolService>();
            var newValue = evt.newValue.GetActiveOptions(stackalloc int[Group.value.length]);
            ToolType type = newValue.Length > 0 ? (ToolType)newValue[0] : ToolType.None;
            if (type == ToolType.None)
                tools.CloseAllToolsWithPanel(); //todo Do we close all tools on toggling off a tool button?
            else
                tools.GetTool(type)?.Open();
        }

        public void ClearWithoutNotify()
        {
            Group.SetValueWithoutNotify(new ToggleButtonGroupState(0ul, Group.value.length));
        }
        
        public void UpdateState()
        {
            if(tools == null) return;
            
            ulong bits = 0ul;
            foreach (var entry in tools.GetAllToolsWithPanel())
            {
                if (entry.IsOpen)
                    bits |= 1ul << (int)tools.GetToolType(entry);
            }
            Group.SetValueWithoutNotify(new ToggleButtonGroupState(bits, Group.value.length));
        }
    }
}