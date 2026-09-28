using System;
using Netherlands3D.Twin;
using Netherlands3D.UI_Toolkit;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    public class WorldUIService : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActionAsset;
        [SerializeField] private FloatingPanelBehaviour[] panelBehaviours;
        [SerializeField] private FloatingButtonBehaviour[] floatingButtonBehaviour;
        private FloatingPanel floatingPanel;
        private VisualElement floatingPanelContent;
        private FloatingPanelBehaviour selectedBehaviour;
        private VisualElement floatingElementsContent;
        
        public VisualElement FloatingElementsContent => floatingElementsContent;

        private void Awake()
        {
            floatingElementsContent = new VisualElement();
        }

        private void Start()
        {
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
            
        }
        
        void OnDisable()
        {
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

        public void OnRightClick(InputAction.CallbackContext ctx)
        {
            Vector2 panelPos = App.UIRoot.GetPanelClickPosition();
            
            if(IsActivePanelClicked(panelPos))
                return;
            
            ClearActivePanel();
            
            //todo we should probably wait one frame here to have all systems updated
            CheckAndSpawnPanel(panelPos);
        }
        
        public void OnLeftClick(InputAction.CallbackContext ctx)
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

        public void AddToFloatingElementsContent(FloatingElement floatingElement)
        {
            floatingElementsContent.Add(floatingElement);
        }

        public void RemoveFromFloatingElementsContent(FloatingElement floatingElement)
        {
            floatingElementsContent.Remove(floatingElement);
        }
    }
}