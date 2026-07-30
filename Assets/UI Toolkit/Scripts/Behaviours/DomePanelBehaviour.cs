using Netherlands3D.Services;
using Netherlands3D.Twin.Layers;
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
        [SerializeField] [TextArea] private string headerText = "Uitsnijden lagen binnen de dome";
        [SerializeField] [TextArea] private string description = "Kies hieronder welke lagen zichtbaar zijn binnen de dome. Verborgen lagen worden niet meegenomen in de uitsnede.";
        
        public override object GetData()
        {
            return ProjectData.Current.RootLayer;
        }
        
        public override VisualElement SpawnFloatingPanelContent(FloatingPanel floatingPanel, params object[] constructorArgs)
        {
            base.SpawnFloatingPanelContent(floatingPanel, constructorArgs);
            var rootLayer = constructorArgs[0] as LayerData;
            MaskingPanel content = new MaskingPanel(rootLayer, MaskingLayerPropertyData.MASKING_DOME_BIT_INDEX);
            content.SetHeader(headerText);
            content.SetDescription(description);
            return content;
        }
        
        public override bool ShouldBeActive()
        {
            //ServiceLocator.GetService<MaskingDomeSpawner>()
            
            return domeTool.IsOpen;
        }
    }
}
