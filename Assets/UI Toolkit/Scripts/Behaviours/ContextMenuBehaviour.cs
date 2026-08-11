using Netherlands3D.Twin;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    public class ContextMenuBehaviour : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActionAsset;
        [SerializeField] private FloatingPanelBehaviour[] panelBehaviours;
        
        private InputAction rightClickAction;
        private InputAction leftClickAction;
        private InputAction longPressAction;
        private InputAction touchAction;
        private FloatingPanel floatingPanel;
        private VisualElement content;
        private FloatingPanelBehaviour selectedBehaviour;

        void OnEnable()
        {
            floatingPanel = new FloatingPanel();
            App.UIRoot.Root.Add(floatingPanel);
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
            Vector2 panelPos = App.UIRoot.GetPanelClickPosition();
            
            if(IsActivePanelClicked(panelPos))
                return;
            
            ClearActivePanel();
            if(App.UIRoot.IsPointerOverUI())
                return;
            
            //todo we should probably wait one frame here to have all systems updated
            CheckAndSpawnPanel(panelPos);
        }
        
        private void OnLeftClick(InputAction.CallbackContext ctx)
        {
            Vector2 panelPos = App.UIRoot.GetPanelClickPosition();
            if(IsActivePanelClicked(panelPos))
                return;
            
            ClearActivePanel();
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