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
            return layers.Where(layer => layer.GetProperty<MaskingLayerPropertyData>() != null).ToDictionary(layer => layer.Id.ToString(), layer => (object)layer); //keep only the maskable layers
        }
        
        public override VisualElement SpawnFloatingPanelContent(FloatingPanel floatingPanel, Dictionary<string,object> data = null)
        {
            base.SpawnFloatingPanelContent(floatingPanel, data);
            DomePanel content = new DomePanel(data);
            return content;
        }
        
        public override bool ShouldBeActive()
        {
            return domeTool.Open;
        }
    }
}
