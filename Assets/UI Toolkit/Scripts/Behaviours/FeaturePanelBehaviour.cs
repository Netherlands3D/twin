using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net.Geometry;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Services;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [CreateAssetMenu(fileName = "FeaturePanelBehaviour", menuName = "ScriptableObjects/FloatingPanelBehaviours/FeaturePanelBehaviour", order = 1)]
    public class FeaturePanelBehaviour : FloatingPanelBehaviour
    {
        //todo improve flow for specific imapping type?
        public override bool ShouldBeActive()
        {
            // ObjectSelectorService objectSelectorService = ServiceLocator.GetService<ObjectSelectorService>();
            // Dictionary<string, IMapping> selectedMappings = objectSelectorService.SelectedMappings;
            // return selectedMappings.Values.Any(m => m is FeatureMapping);
            
            ObjectSelectorService objectSelectorService = ServiceLocator.GetService<ObjectSelectorService>();
            Dictionary<string, IMapping> selectedMappings = objectSelectorService.SelectedMappings;
            
            IEnumerable<FeatureMapping> featureMappings = selectedMappings.Values.OfType<FeatureMapping>();
            foreach (FeatureMapping featureMapping in featureMappings)
                if (featureMapping.Feature != null && (featureMapping.Feature.Geometry is Polygon || featureMapping.Feature.Geometry is MultiPolygon))
                    return true;
            
            return false;
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
            content = new FeaturePanel(constructorArgs[0] as Dictionary<string, IMapping>);
            FeaturePanel panel = content as FeaturePanel;
            panel.OnClose.AddListener(CloseFloatingPanel);
            return content;
        }

        private void CloseFloatingPanel()
        {
            floatingPanel.OnClose.Invoke();
            ObjectSelectorService objectSelectorService = ServiceLocator.GetService<ObjectSelectorService>();
            objectSelectorService.Deselect();
        }

        public override void Dispose()
        {
            FeaturePanel panel = content as FeaturePanel;
            panel.OnClose.RemoveListener(CloseFloatingPanel);
            base.Dispose();
        }
    }
}
