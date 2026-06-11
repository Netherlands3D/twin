using System;
using System.Collections.Generic;
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
            
        }

        private void EnsureService()
        {
            if (tools == null)
            {
                tools = Services.ServiceLocator.GetService<ToolService>();
                tools.AnyToolClosed.AddListener(UpdateState);
                tools.AnyToolOpened.AddListener(UpdateState);
            }
        }
        
        public void UpdateState()
        {
            if(tools == null) return;
    
            foreach (var entry in buttons)
            {
                var tool = tools.GetTool(entry.ToolType);
                if (tool != null && tool.IsOpen)
                    entry.Button.AddToClassList("active");
                else
                    entry.Button.RemoveFromClassList("active");
            }
        }
    }
}