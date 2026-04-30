using System.Collections.Generic;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Services;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [CreateAssetMenu(fileName = "HideObjectPanelBehaviour", menuName = "ScriptableObjects/FloatingPanelBehaviours/HideObjectPanelBehaviour", order = 1)]
    public class HideObjectPanelBehaviour : FloatingPanelBehaviour
    {
        public override bool ShouldBeActive()
        {
            ObjectSelectorService objectSelectorService = ServiceLocator.GetService<ObjectSelectorService>();
            Dictionary<string, IMapping> selectedMappings = objectSelectorService.SelectedMappings;
            if(selectedMappings.Count == 0) return false;

            return true;
        }

        public override object GetData()
        {
             ObjectSelectorService objectSelectorService = ServiceLocator.GetService<ObjectSelectorService>();
             Dictionary<string, IMapping> selectedMappings = objectSelectorService.SelectedMappings;
            return selectedMappings;
        }

        public override VisualElement SpawnFloatingPanelContent(FloatingPanel floatingPanel, params object[] constructorArgs)
        {
            base.SpawnFloatingPanelContent(floatingPanel);
            content = new HideObjectPanel(constructorArgs[0] as Dictionary<string, IMapping>);
            HideObjectPanel panel = content as HideObjectPanel;
            panel.OnClose.AddListener(CloseFloatingPanel);
            return content;
        }

        private void CloseFloatingPanel()
        {
            floatingPanel.OnClose.Invoke();
            ObjectSelectorService objectSelectorService = ServiceLocator.GetService<ObjectSelectorService>();
            objectSelectorService.SubObjectSelector.HideSelectedMappings();
            objectSelectorService.Deselect();
        }

        public override void Dispose()
        {
            HideObjectPanel panel = content as HideObjectPanel;
            panel.OnClose.RemoveListener(CloseFloatingPanel);
            base.Dispose();
        }
    }
}
