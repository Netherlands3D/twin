using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.Properties;
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

        public override Dictionary<string, object> GetData()
        {
             ObjectSelectorService objectSelectorService = ServiceLocator.GetService<ObjectSelectorService>();
             Dictionary<string, IMapping> selectedMappings = objectSelectorService.SelectedMappings;
            return selectedMappings.ToDictionary(kvp => kvp.Key, kvp => (object)null);
        }

        public override VisualElement SpawnFloatingPanelContent(FloatingPanel floatingPanel, Dictionary<string,object> data = null)
        {
            base.SpawnFloatingPanelContent(floatingPanel, data);
            content = new HideObjectPanel(data);
            HideObjectPanel panel = content as HideObjectPanel;
            panel.Button.clicked += CloseFloatingPanel;
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
            panel.Button.clicked -= CloseFloatingPanel;
            base.Dispose();
        }

        public override void LoadProperties(List<LayerPropertyData> properties)
        {
            //has no data for now
        }
    }
}
