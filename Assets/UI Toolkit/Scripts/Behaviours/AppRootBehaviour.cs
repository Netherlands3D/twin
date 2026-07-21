using System;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Functionalities;
using System.Collections.Generic;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Netherlands3D
{
    [RequireComponent(typeof(UIDocument))]
    public class AppRootBehaviour : MonoBehaviour
    {
        public VisualElement Root => appRoot;

        private UIDocument appDocument;
        private VisualElement appRoot;

        //the excuted order of this script should be executed very early to ensure the presence of the approot. 
        private void Awake()
        {
            appDocument = GetComponent<UIDocument>();
            appRoot = appDocument?.rootVisualElement;
        }

        private void Start()
        {
            DisableFPVUI();
        }

        //todo: in the future we might want to create a list of huds we can switch between, so we avoid multiple true/false permutations, but for now we only have 2, so this is not needed yet
        public void DisableFPVUI()
        {
            appRoot.Q<DefaultHUD>().EnableInClassList(UtilityClassConstants.HIDDEN, false);
            appRoot.Q<FPVHUD>().EnableInClassList(UtilityClassConstants.HIDDEN, true);
        }

        public void EnableFPVUI()
        {
            appRoot.Q<FPVHUD>().EnableInClassList(UtilityClassConstants.HIDDEN, false);
            appRoot.Q<DefaultHUD>().EnableInClassList(UtilityClassConstants.HIDDEN, true);
        }

        /// <summary>
        /// Some UI elements should behave differently (i.e. be shown) when a functionality is enabled. This code
        /// will add a class on the top-most level so that each component can decide how to respond when a functionality
        /// is enabled.
        /// </summary>
        public void EnableFunctionality(Functionality functionality)
        {
            appRoot.AddToClassList("app--functionality-" + functionality.Id);
        }

        /// <summary>
        /// Removes the global class that allows part of the application's UI to respond to the functionality being
        /// disabled.
        /// </summary>
        public void DisableFunctionality(Functionality functionality)
        {
            appRoot.RemoveFromClassList("app--functionality-" + functionality.Id);
        }

        public Vector2 GetPanelClickPosition()
        {
            var screenPos = Pointer.current.position.ReadValue();
            screenPos.y = Screen.height - screenPos.y;
            return RuntimePanelUtils.ScreenToPanel(appRoot.panel, screenPos);
        }

        public bool IsOverUI(Vector2 screenPos)
        {
            Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(appRoot.panel, screenPos);
            VisualElement picked = appRoot.panel.Pick(panelPosition);
            
            return picked != null;
        }

        public bool IsUIClicked()
        {
            return IsOverUI(GetPanelClickPosition());
        }
    }
}