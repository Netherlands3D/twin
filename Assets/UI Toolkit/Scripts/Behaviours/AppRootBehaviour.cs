using Netherlands3D.Twin;
using Netherlands3D.Twin.Functionalities;
using Netherlands3D.UI_Toolkit;
using System.Collections.Generic;
using Netherlands3D.UI.Components;
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
        private VisualElement leftSection;
        private VisualElement propertiesZone;
        private VisualElement toolbarNavigation;
        private VisualElement toolbarToolbox;
        private VisualElement toolbarToolboxFPV;

        //the excuted order of this script should be executed very early to ensure the presence of the approot. 
        private void Awake()
        {
            appDocument = GetComponent<UIDocument>();
            appRoot = appDocument?.rootVisualElement.Q("App");
            leftSection = appRoot.Q("LeftSection");
            propertiesZone = appRoot.Q("PropertiesZone");
            toolbarNavigation = appRoot.Q<ToolbarNavigation>();
            toolbarToolbox = appRoot.Q<ToolbarToolbox>();
            toolbarToolboxFPV = appRoot.Q<ToolbarToolboxFPV>();
        }

        public void ShowDefaultUI() //todo: rename/move this function?
        {
            // appRoot.RemoveFromClassList(UtilityClassConstants.HIDDEN);
            leftSection.RemoveFromClassList(UtilityClassConstants.HIDDEN);
            propertiesZone.RemoveFromClassList(UtilityClassConstants.HIDDEN);
            toolbarNavigation.RemoveFromClassList(UtilityClassConstants.HIDDEN);
            toolbarToolbox.RemoveFromClassList(UtilityClassConstants.HIDDEN);
            toolbarToolboxFPV.AddToClassList(UtilityClassConstants.HIDDEN);
        }

        public void HideDefaultUI()
        {
            // appRoot.AddToClassList(UtilityClassConstants.HIDDEN);
            leftSection.AddToClassList(UtilityClassConstants.HIDDEN);
            propertiesZone.AddToClassList(UtilityClassConstants.HIDDEN);
            toolbarNavigation.AddToClassList(UtilityClassConstants.HIDDEN);
            toolbarToolbox.AddToClassList(UtilityClassConstants.HIDDEN);
            toolbarToolboxFPV.RemoveFromClassList(UtilityClassConstants.HIDDEN);
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

        public bool ClickedUI(Vector2 screenPos)
        {
            var picked = appRoot.panel.Pick(screenPos);
            // block if we hit something other than the root background
            if (picked != null && picked != appRoot)
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
    }
}
