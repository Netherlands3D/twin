using System.Collections.Generic;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Services;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    public class FloatingPanelSpawner : MonoBehaviour
    {
        private VisualElement root;

        void Awake()
        {
            var doc = GetComponent<UIDocument>();
            root = doc.rootVisualElement;

            root.pickingMode = PickingMode.Position;
            root.RegisterCallback<MouseDownEvent>(OnClick, TrickleDown.TrickleDown);
        }
        
        private void SpawnFloatingPanel<T>(Vector2 screenPos, object context = null) where T : FloatingPanel, new()
        {
            var panel = new T();
            panel.Initialize(screenPos, context);
            panel.SetPosition(screenPos);
            root.Add(panel);
        }

        private void OnClick(MouseDownEvent evt)
        {
            List<IMapping> selectedMappings = ServiceLocator.GetService<ObjectSelectorService>().SubObjectSelector.SelectedMappings;
            SpawnFloatingPanel<HideObjectPanel>(evt.mousePosition, selectedMappings);
        }
    }
}
