using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [CreateAssetMenu(fileName = "ObjectPanelBehaviour", menuName = "ScriptableObjects/FloatingPanelBehaviours/ObjectPanelBehaviour", order = 1)]
    public class ObjectPanelBehaviour : FloatingPanelBehaviour
    {
        private LayerData target;
        
        public override bool ShouldBeActive()
        {
            ObjectSelectorService objectSelectorService = ServiceLocator.GetService<ObjectSelectorService>();
            if (objectSelectorService.SelectedVisualisation != null)
            {
                target = objectSelectorService.SelectedVisualisation.LayerData;
            }
            return  objectSelectorService.SelectedVisualisation != null;
        }

        public override object GetData()
        {
            return target;
        }

        public override VisualElement SpawnFloatingPanelContent(FloatingPanel floatingPanel, params object[] constructorArgs)
        {
            base.SpawnFloatingPanelContent(floatingPanel);
            content = new ObjectPanel(constructorArgs[0] as LayerData);
            ObjectPanel panel = content as ObjectPanel;
            panel.OnClose.AddListener(CloseFloatingPanel);
            return content;
        }

        private void CloseFloatingPanel()
        {
            floatingPanel.OnClose.Invoke();
            var transformInterfaceToggle = ServiceLocator.GetService<TransformHandleInterfaceToggle>();
            transformInterfaceToggle.ClearTransformTarget();
        }

        public override void Dispose()
        {
            ObjectPanel panel = content as ObjectPanel;
            panel.OnClose.RemoveListener(CloseFloatingPanel);
            base.Dispose();
        }
    }
}
