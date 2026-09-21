using System.Collections.Generic;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ToolbarMain : VisualElement
    {
        private ToolService tools;
        private List<ToolButton> buttons;

        public ToolbarMain()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            buttons = this.Query<ToolButton>().ToList();
            foreach(var entry in buttons)
                entry.RegisterCallback<ClickEvent>(evt =>
                {
                    EnsureService();
                    ToolType type = entry.ToolType;
                    bool isActive = entry.Button.ClassListContains("active");
                    foreach(var b in buttons)
                        b.Button.RemoveFromClassList("active");
        
                    if (!isActive)
                    {
                        entry.Button.AddToClassList("active");
                        tools.GetTool(type)?.Open();
                    }
                    else
                    {
                        tools.CloseAllToolsWithPanel();
                    }
                });
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            UpdateState();
        }

        private void EnsureService()
        {
            if (tools == null)
            {
                tools = Services.ServiceLocator.GetService<ToolService>();
                tools.AnyToolClosed.AddListener(UpdateState);
                tools.AnyToolOpened.AddListener(UpdateState);
                tools.AnyToolAvailabilityChanged.AddListener(UpdateState);
            }
        }

        private void UpdateState(bool _)
        {
            UpdateState();
        }
        
        private void UpdateState()
        {
            EnsureService();
            
            if(tools == null) return;
    
            foreach (var entry in buttons)
            {
                var tool = tools.GetTool(entry.ToolType);

                var isHidden = tool != null && !tool.Available;
                var isOpen = tool != null && tool.Available && tool.IsOpen;

                entry.Button.EnableInClassList(UtilityClassConstants.HIDDEN, isHidden);
                entry.Button.EnableInClassList("active", isOpen);
            }
        }
    }
}