using System.Collections.Generic;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [CreateAssetMenu(fileName = "ObjectPanelBehaviour", menuName = "ScriptableObjects/FloatingPanelBehaviours/ObjectPanelBehaviour", order = 1)]
    public class ObjectPanelBehaviour : FloatingPanelBehaviour
    {
        private static readonly List<RaycastResult> results = new(8);
        private HierarchicalObjectLayerGameObject target;
        
        public override bool ShouldBeActive()
        {
            results.Clear();
            target = null;
            var pointerPos = Pointer.current.position.ReadValue();
            var pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = pointerPos;
            EventSystem.current.RaycastAll(pointerData, results);
            ObjectSelectorService selectionService = ServiceLocator.GetService<ObjectSelectorService>();
            if(!selectionService.IsAnyToolActive()) return false;
            foreach (var raycast in results)
            {
                if (!raycast.gameObject.TryGetComponent(out HierarchicalObjectLayerGameObject t))
                {
                    t = raycast.gameObject.GetComponentInParent<HierarchicalObjectLayerGameObject>();
                }
                if (t != null)
                {
                    target = t;
                    t.LayerData.SelectLayer(true);
                    return true;
                }
            }
            return false;
        }

        public override object GetData()
        {
            return target;
        }

        public override VisualElement SpawnFloatingPanelContent(FloatingPanel floatingPanel, params object[] constructorArgs)
        {
            base.SpawnFloatingPanelContent(floatingPanel);
            content = new ObjectPanel(constructorArgs[0] as HierarchicalObjectLayerGameObject);
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
