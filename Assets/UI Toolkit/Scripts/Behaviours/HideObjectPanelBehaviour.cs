using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Services;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    public class HideObjectPanelBehaviour : FloatingPanelBehaviour
    {
        private ObjectSelectorService objectSelectorService;
        private HideObjectPanel panel;
        private FloatingPanel floatingPanel;
        
        private void Awake()
        {
            objectSelectorService = ServiceLocator.GetService<ObjectSelectorService>();
        }

        public override bool ShouldBeActive()
        {
            Dictionary<string, IMapping> selectedMappings = objectSelectorService.SelectedMappings;
            if(selectedMappings.Count == 0) return false;

            return true;
        }

        public override Dictionary<string, object> GetData()
        {
            Dictionary<string, IMapping> selectedMappings = objectSelectorService.SelectedMappings;
            return selectedMappings.ToDictionary(kvp => kvp.Key, kvp => (object)null);
        }

        public override VisualElement SpawnFloatingPanelContent(FloatingPanel floatingPanel, Dictionary<string,object> data = null)
        {
            this.floatingPanel = floatingPanel; 
            panel = new HideObjectPanel(data);
            panel.Button.clicked += CloseFloatingPanel;
            return panel;
        }

        private void CloseFloatingPanel()
        {
            floatingPanel.OnClose.Invoke();
            objectSelectorService.SubObjectSelector.HideSelectedMappings();
            objectSelectorService.Deselect();
        }

        public override void Dispose()
        {
            panel.Button.clicked -= CloseFloatingPanel;
            floatingPanel = null;
            panel = null;
        }
    }
}
