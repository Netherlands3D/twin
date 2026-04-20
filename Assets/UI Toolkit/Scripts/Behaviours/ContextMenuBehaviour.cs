using System.Collections.Generic;
using Netherlands3D.Twin;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    public class ContextMenuBehaviour : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActionAsset;
        [SerializeField] private FloatingPanelBehaviour[] panelBehaviours;
        
        private VisualElement root;
        private InputAction rightClickAction;
        private InputAction leftClickAction;
        private InputAction longPressAction;
        private InputAction touchAction;
        private FloatingPanel floatingPanel;
        private VisualElement content;
        private FloatingPanelBehaviour selectedBehaviour;

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            floatingPanel = new FloatingPanel();
            root.Add(floatingPanel);
            floatingPanel.OnClose.AddListener(ClearActivePanel);
            floatingPanel.SetEnabled(false);
            var map = inputActionAsset.FindActionMap("Camera", true);
            rightClickAction = map.FindAction("RightClick", true);
            leftClickAction = map.FindAction("LeftClick", true);
            longPressAction = map.FindAction("LongPress", true);
            touchAction = map.FindAction("Touch", true);
            
            rightClickAction.performed += OnRightClick;
            leftClickAction.performed += OnLeftClick;
            longPressAction.performed += OnRightClick;
            touchAction.performed += OnLeftClick;
        }

        void OnDisable()
        {
            rightClickAction.performed -= OnRightClick;
            leftClickAction.performed -= OnLeftClick;
            longPressAction.performed -= OnRightClick;
            touchAction.performed -= OnLeftClick;
            ClearActivePanel();
            floatingPanel = null;
        }

        public void ClearActivePanel()
        {
            if (content == null)
                return;

            selectedBehaviour?.Dispose();
            floatingPanel.Remove(content);
            content = null;
            floatingPanel.SetEnabled(false);
        }

        private void OnRightClick(InputAction.CallbackContext ctx)
        {
            Vector2 panelPos = GetPanelClickPosition();
            ClickedUI(panelPos);
            
            if(IsActivePanelClicked(panelPos))
                return;
            
            ClearActivePanel();
            if(ClickedUI(panelPos))
                return;
            
            CheckAndSpawnPanel(panelPos);
        }
        
        private void OnLeftClick(InputAction.CallbackContext ctx)
        {
            Vector2 panelPos = GetPanelClickPosition();
            if(IsActivePanelClicked(panelPos))
                return;
            
            ClearActivePanel();
        }

        private Vector2 GetPanelClickPosition()
        {
            var screenPos = Pointer.current.position.ReadValue();
            screenPos.y = Screen.height - screenPos.y;
            return RuntimePanelUtils.ScreenToPanel(root.panel, screenPos);
        }

        private bool ClickedUI(Vector2 screenPos)
        {
            var picked = root.panel.Pick(screenPos);
            // block if we hit something other than the root background
            if (picked != null && picked != root)
                return true;
            
            var pointerPos = Pointer.current.position.ReadValue();
            // block if we hit anything except the ClickNothingPanel . todo: remove this once transition to UI Toolkit is completed
            var pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = pointerPos;
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

            if (clickedInWorld)
            {
                return false;
            }
            
            return true;
        }

        public bool IsUIClicked()
        {
            return ClickedUI(GetPanelClickPosition());
        }

        private bool IsActivePanelClicked(Vector2 screenPos)
        {
            if(floatingPanel == null) return false;
            
            var picked = floatingPanel.panel.Pick(screenPos);
            return picked != null && floatingPanel.Contains(picked);
        }
        
        private void CheckAndSpawnPanel(Vector2 screenPos)
        {
            foreach (var panelBehaviour in panelBehaviours)
            {
                if(!panelBehaviour.ShouldBeActive()) continue;

                selectedBehaviour = panelBehaviour;
                var data = panelBehaviour.GetData();
                content = panelBehaviour.SpawnFloatingPanelContent(floatingPanel, data);
                floatingPanel.SetEnabled(true);
                floatingPanel.Add(content);
                floatingPanel.SetPosition(screenPos);
                break;
            }
        }
    }
}