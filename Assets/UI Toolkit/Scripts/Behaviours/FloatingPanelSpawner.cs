using System.Collections.Generic;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Services;
using Netherlands3D.Twin;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    public class FloatingPanelSpawner : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActionAsset;
        private VisualElement root;
        private InputAction rightClickAction;

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            var map = inputActionAsset.FindActionMap("UI", true);
            rightClickAction = map.FindAction("RightClick", true);

            rightClickAction.performed += OnRightClick;
            rightClickAction.Enable();
        }

        void OnDisable()
        {
            rightClickAction.performed -= OnRightClick;
            rightClickAction.Disable();
        }

        private void SpawnFloatingPanel<T>(Vector2 screenPos, object context = null) where T : FloatingPanel, new()
        {
            var panel = new T();
            panel.Initialize(screenPos, context);
            panel.SetPosition(screenPos);
            root.Add(panel);
        }

        private void OnRightClick(InputAction.CallbackContext ctx)
        {
            var screenPos = Pointer.current.position.ReadValue();
            // block if we hit anything except the ClickNothingPanel . todo: remove this once transition to UI Toolkit is completed
            var pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = screenPos;
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            if (results.Count > 0 && !results[0].gameObject.GetComponent<ClickNothingPlane>())
                return;
            
            screenPos.y = Screen.height - screenPos.y;
            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(root.panel, screenPos);
            var picked = root.panel.Pick(panelPos);

            // block if we hit something other than the root background
            if (picked != null && picked != root)
                return;
            
            List<IMapping> selectedMappings = ServiceLocator.GetService<ObjectSelectorService>().SubObjectSelector.SelectedMappings;
            SpawnFloatingPanel<HideObjectPanel>(panelPos, selectedMappings);
        }
    }
}
