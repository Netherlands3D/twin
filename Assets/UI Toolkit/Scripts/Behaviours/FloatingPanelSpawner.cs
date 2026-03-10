using System.Collections.Generic;
using System.Linq;
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
        private InputAction leftClickAction;
        private FloatingPanel activePanel;

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            var map = inputActionAsset.FindActionMap("Camera", true);
            rightClickAction = map.FindAction("RightClick", true);
            leftClickAction = map.FindAction("LeftClick", true);

            rightClickAction.performed += OnRightClick;
            rightClickAction.Enable();

            leftClickAction.performed += OnLeftClick;
            leftClickAction.Enable();
        }

        void OnDisable()
        {
            rightClickAction.performed -= OnRightClick;
            rightClickAction.Disable();
            
            leftClickAction.performed -= OnLeftClick;
            leftClickAction.Disable();
        }

        private void SpawnFloatingPanel<T>(Vector2 screenPos, Dictionary<string, object> data = null) where T : FloatingPanel, new()
        {
            activePanel = new T();
            activePanel.Initialize(screenPos, data);
            activePanel.SetPosition(screenPos);
            activePanel.OnClose.AddListener(ClearActivePanel);
            root.Add(activePanel);
        }

        public void ClearActivePanel()
        {
            if (activePanel == null)
                return;

            activePanel.OnClose.RemoveListener(ClearActivePanel);
            root.Remove(activePanel);
            activePanel = null;
        }

        private void OnRightClick(InputAction.CallbackContext ctx)
        {
            var screenPos = Pointer.current.position.ReadValue();
            // block if we hit anything except the ClickNothingPanel . todo: remove this once transition to UI Toolkit is completed
            var pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = screenPos;
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            bool clickedInWorld = false;
            foreach (var result in results)
            {
                if (result.gameObject.layer == LayerMask.NameToLayer("UI"))
                    break;
                if (result.gameObject.GetComponent<ClickNothingPlane>())
                    clickedInWorld = true;
            }

            if (!clickedInWorld)
            {
                Debug.Log("not clicked in world");
                return;
            }

            screenPos.y = Screen.height - screenPos.y;
            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(root.panel, screenPos);
            var picked = root.panel.Pick(panelPos);

            // block if we hit something other than the root background
            if (picked != null && picked != root)
                return;
            
            ClearActivePanel();
            CheckAndSpawnPanel(panelPos);
        }

        private void OnLeftClick(InputAction.CallbackContext ctx)
        {
            ClearActivePanel();
        }

        private void CheckAndSpawnPanel(Vector2 screenPos)
        {
           SpawnHideObjectPanel(screenPos);
        }

        public void SpawnHideObjectPanel(Vector2 screenPos)
        {
            Dictionary<string, IMapping> selectedMappings = ServiceLocator.GetService<ObjectSelectorService>().SelectedMappings;
            if(selectedMappings.Count == 0) return;
            
            Dictionary<string, object> data = selectedMappings.ToDictionary(kvp => kvp.Key, kvp => (object)null);
            SpawnFloatingPanel<HideObjectPanel>(screenPos, data);
        }
    }
}