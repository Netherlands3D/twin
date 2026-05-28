using System;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ToolbarMain : VisualElement
    {
        // public enum Tool
        // {
        //     Layer = 0,
        //     Library = 1,
        //     Add = 2,
        //     Search = 3,
        //     SunPosition = 4,
        //     DownloadTile = 5
        // }

        public ToggleButtonGroup Group => this.Q<ToggleButtonGroup>("ButtonGroup");

        // public event Action OnLayerToolSelected;
        // public event Action OnLibraryToolSelected;
        // public event Action OnAddToolSelected;
        // public event Action OnSearchToolSelected;
        // public event Action OnSunPositionToolSelected;
        // public event Action OnDownloadToolSelected;
        // public event Action OnToolDeselected;
        
        private ToolService tools;

        public ToolbarMain()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            tools = Services.ServiceLocator.GetService<ToolService>();
            RegisterCallback<AttachToPanelEvent>(NotifyAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(evt => tools.ClearWithoutNotify.RemoveListener(ClearWithoutNotify));
        }

        private void NotifyAttachedToPanel(AttachToPanelEvent _)
        {
            Group.RegisterValueChangedCallback(NotifyValueChanged);
            tools.ClearWithoutNotify.AddListener(ClearWithoutNotify);
            ClearWithoutNotify();
        }

        private void NotifyValueChanged(ChangeEvent<ToggleButtonGroupState> evt)
        {
            var newValue = evt.newValue.GetActiveOptions(stackalloc int[Group.value.length]);
            ToolType type = newValue.Length > 0 ? (ToolType)newValue[0] : ToolType.None;
            Services.ServiceLocator.GetService<ToolService>().NotifyTool(type);
            // switch (newButton)
            // {
            //     case Tool.Layer: OnLayerToolSelected?.Invoke(); break;
            //     case Tool.Library: OnLibraryToolSelected?.Invoke(); break;
            //     case Tool.Add: OnAddToolSelected?.Invoke(); break;
            //     case Tool.Search: OnSearchToolSelected?.Invoke(); break;
            //     case Tool.SunPosition: OnSunPositionToolSelected?.Invoke(); break;
            //     case Tool.DownloadTile: OnDownloadToolSelected?.Invoke(); break;
            //     default: OnToolDeselected?.Invoke(); break;
            // }
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