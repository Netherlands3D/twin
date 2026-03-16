using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Services;
using UnityEngine;

namespace Netherlands3D.UI.Panels
{
    public class HideObjectPanelBehaviour : FloatingPanelBehaviour<HideObjectPanel>
    {
        private ObjectSelectorService objectSelectorService;
        
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

        public override FloatingPanel SpawnFloatingPanel(Vector2 screenPos, Dictionary<string,object> data = null)
        {
            HideObjectPanel panel = base.SpawnFloatingPanel(screenPos, data) as HideObjectPanel;
            panel.OnClose.AddListener(objectSelectorService.SubObjectSelector.HideSelectedMappings);
            panel.OnClose.AddListener(objectSelectorService.Deselect);
            return panel;
        }
    }
}
