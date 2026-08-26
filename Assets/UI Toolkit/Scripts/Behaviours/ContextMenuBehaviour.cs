using System;
using Netherlands3D.Services;
using Netherlands3D.Twin;
using Netherlands3D.UI_Toolkit;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    public class ContextMenuBehaviour : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActionAsset;
        [SerializeField] private FloatingPanelBehaviour[] panelBehaviours;
        [SerializeField] private FloatingButtonBehaviour[] floatingButtonBehaviour;

        private InputAction rightClickAction;
        private InputAction leftClickAction;
        private InputAction longPressAction;
        private InputAction touchAction;
        private FloatingPanel floatingPanel;
        private VisualElement floatingPanelContent;
        private FloatingPanelBehaviour selectedBehaviour;
        private VisualElement floatingElementsContent;

        private void Start()
        {
            floatingElementsContent = new VisualElement();
            App.UIRoot.Root.Add(floatingElementsContent);
            
            foreach (var buttonBehaviour in floatingButtonBehaviour)
            {             
                buttonBehaviour.Initialize(floatingElementsContent);
            }
        }

        void OnEnable()
        {
            floatingPanel = new FloatingPanel();
            App.UIRoot.Root.Add(floatingPanel);
            floatingPanel.OnClose.AddListener(ClearActivePanel);
            floatingPanel.EnableInClassList(UtilityClassConstants.HIDDEN, true);
            
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

        private void OnDestroy()
        {
            foreach (var buttonBehaviour in floatingButtonBehaviour)
            {
                buttonBehaviour.Dispose();
            }
        }

        private void Update()
        {
            foreach (var buttonBehaviour in floatingButtonBehaviour)
            {
                buttonBehaviour.UpdateBehaviour();
            }
        }

        public void ClearActivePanel()
        {
            if (floatingPanelContent == null)
                return;

            selectedBehaviour?.Dispose();
            floatingPanel.Remove(floatingPanelContent);
            floatingPanelContent = null;
            floatingPanel.EnableInClassList(UtilityClassConstants.HIDDEN, true);
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
                floatingPanelContent = panelBehaviour.SpawnFloatingPanelContent(floatingPanel, data);
                floatingPanel.EnableInClassList(UtilityClassConstants.HIDDEN, false);
                floatingPanel.Add(floatingPanelContent);
                floatingPanel.SetPosition(screenPos);
                floatingPanel.BringToFront();
                break;
            }
        }
    }
}