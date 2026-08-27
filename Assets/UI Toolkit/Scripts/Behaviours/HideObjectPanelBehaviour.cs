using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Services;
using Netherlands3D.SubObjects;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [CreateAssetMenu(fileName = "HideObjectPanelBehaviour", menuName = "ScriptableObjects/FloatingPanelBehaviours/HideObjectPanelBehaviour", order = 1)]
    public class HideObjectPanelBehaviour : FloatingPanelBehaviour
    {
        //todo improve flow for specific imapping type?
        public override bool ShouldBeActive()
        {
            ObjectSelectorService objectSelectorService = ServiceLocator.GetService<ObjectSelectorService>();
            Dictionary<string, IMapping> selectedMappings = objectSelectorService.SelectedMappings;
            
            return selectedMappings.Values.Any(m => m is MeshMapping);
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
            ObjectSelectorService objectSelectorService = ServiceLocator.GetService<ObjectSelectorService>();
            objectSelectorService.SelectSubObjectWithBagId.AddListener(OnUpdateMappings);
            HideObjectPanel panel = content as HideObjectPanel;
            panel.OnClose.AddListener(CloseFloatingPanel);
            return content;
        }

        private void OnUpdateMappings(MeshMapping mapping, string bagId)
        {
            HideObjectPanel panel = content as HideObjectPanel;
            panel.UpdateContent();
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
            
            ObjectSelectorService objectSelectorService = ServiceLocator.GetService<ObjectSelectorService>();
            objectSelectorService.SelectSubObjectWithBagId.RemoveListener(OnUpdateMappings); //todo: if the panel is destroyed outside of this script, the OnUpdateMappings would give a NullReferenceException
        }
    }
}
