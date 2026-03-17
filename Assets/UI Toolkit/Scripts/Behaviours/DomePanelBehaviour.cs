using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Tools;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [CreateAssetMenu(fileName = "DomePanelBehaviour", menuName = "ScriptableObjects/FloatingPanelBehaviours/DomePanelBehaviour", order = 1)]
    public class DomePanelBehaviour : FloatingPanelBehaviour
    {
        [SerializeField] private Tool domeTool;
        public override Dictionary<string, object> GetData()
        {
            var layers = ProjectData.Current.RootLayer.GetFlatHierarchy();
            return layers.ToDictionary(layer => layer.Id.ToString(), layer => (object)layer);
        }
        
        public override VisualElement SpawnFloatingPanelContent(FloatingPanel floatingPanel, Dictionary<string,object> data = null)
        {
            base.SpawnFloatingPanelContent(floatingPanel, data);
            DomePanel content = new DomePanel(data);
            return content;
        }

        public override void Dispose()
        {
        }

        public override void LoadProperties(List<LayerPropertyData> properties)
        {
            //Dome is not a layer so it has no properties to load. We also do not want to keep the selected layers that the dome affects between sessions.
        }
        
        public override bool ShouldBeActive()
        {
            return domeTool.Open;
        }
    }
}
