using Netherlands3D.Twin.Functionalities;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using EditableNameField = Netherlands3D.UI.Components.EditableNameField;

namespace Netherlands3D
{
    [RequireComponent(typeof(UIDocument))]
    public class AppRootBehaviour : MonoBehaviour
    {
        private const string DashboardFunctionalityClass = "app--functionality-dashboarding";
        private const string DashboardOpenClass = "app--dashboard-open";

        public VisualElement Root => appRoot;

        private UIDocument appDocument;
        private VisualElement appRoot;
        private Button closeDashboardButton;
        private VisualElement previousFocus;
        private Functionality dashboardFunctionality;
        private InputAction dashboardShortcut;

        //the excuted order of this script should be executed very early to ensure the presence of the approot. 
        private void Awake()
        {
            appDocument = GetComponent<UIDocument>();
            appRoot = appDocument?.rootVisualElement;

            var dashboardOverlay = appRoot?.Q("DashboardOverlay");
            closeDashboardButton = dashboardOverlay?.Q<Button>("CloseDashboard");
            if (closeDashboardButton == null) return;

            closeDashboardButton.clicked += CloseDashboard;
            dashboardShortcut = new InputAction("Toggle Dashboard Functionality", InputActionType.Button);
            dashboardShortcut.AddCompositeBinding("OneModifier")
                .With("modifier", "<Keyboard>/shift")
                .With("binding", "<Keyboard>/f8");
            dashboardShortcut.performed += OnDashboardShortcut;
        }

        private void OnEnable()
        {
            dashboardShortcut?.Enable();
        }

        private void OnDisable()
        {
            dashboardShortcut?.Disable();
            CloseDashboard();
        }

        private void OnDestroy()
        {
            if (closeDashboardButton != null)
                closeDashboardButton.clicked -= CloseDashboard;

            if (dashboardShortcut != null)
            {
                dashboardShortcut.performed -= OnDashboardShortcut;
                dashboardShortcut.Dispose();
            }
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
            var functionalityClass = "app--functionality-" + functionality.Id;
            appRoot.AddToClassList(functionalityClass);
            if (functionalityClass == DashboardFunctionalityClass)
                dashboardFunctionality = functionality;
        }

        /// <summary>
        /// Removes the global class that allows part of the application's UI to respond to the functionality being
        /// disabled.
        /// </summary>
        public void DisableFunctionality(Functionality functionality)
        {
            var functionalityClass = "app--functionality-" + functionality.Id;
            appRoot.RemoveFromClassList(functionalityClass);
            if (functionalityClass == DashboardFunctionalityClass)
            {
                dashboardFunctionality = functionality;
                CloseDashboard();
            }
        }

        private void OnDashboardShortcut(InputAction.CallbackContext _)
        {
            if (dashboardFunctionality == null) return;

            var focusedElement = appRoot.focusController?.focusedElement as VisualElement;
            if (IsTextInputFieldFocused(focusedElement)) return;

            if (appRoot.Query<EditableNameField>().ToList().Exists(IsVisibleNameFieldBeingEdited)) return;

            dashboardFunctionality.IsEnabled = !dashboardFunctionality.IsEnabled;
        }

        internal void OpenDashboard(VisualElement focusOnClose)
        {
            if (closeDashboardButton == null || !isActiveAndEnabled) return;
            if (!appRoot.ClassListContains(DashboardFunctionalityClass) || appRoot.ClassListContains(DashboardOpenClass)) return;

            previousFocus = focusOnClose;
            appRoot.AddToClassList(DashboardOpenClass);
            // Wait for the close button to become visible before moving focus.
            closeDashboardButton.schedule.Execute(() =>
            {
                if (appRoot.ClassListContains(DashboardOpenClass))
                    closeDashboardButton.Focus();
            });
        }

        private static bool IsVisibleNameFieldBeingEdited(EditableNameField field)
        {
            // Inline name fields can stay in edit mode after losing focus while Shift is held.
            if (!field.IsEditing || !field.visible) return false;

            for (VisualElement element = field; element != null; element = element.parent)
            {
                if (element.resolvedStyle.display == DisplayStyle.None)
                    return false;
            }

            return true;
        }

        private static bool IsTextInputFieldFocused(VisualElement element)
        {
            for (var current = element; current != null; current = current.parent)
            {
                for (var type = current.GetType(); type != null; type = type.BaseType)
                {
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(TextInputBaseField<>))
                        return true;
                }
            }

            return false;
        }

        private void CloseDashboard()
        {
            if (appRoot?.ClassListContains(DashboardOpenClass) != true) return;

            appRoot.RemoveFromClassList(DashboardOpenClass);
            var elementToFocus = previousFocus;
            previousFocus = null;
            if (elementToFocus?.panel == null) return;

            // Wait for the viewer UI to become visible again before restoring focus.
            appRoot.schedule.Execute(() =>
            {
                if (!appRoot.ClassListContains(DashboardOpenClass) && elementToFocus.panel == appRoot.panel)
                    elementToFocus.Focus();
            });
        }

        public Vector2 GetPanelClickPosition()
        {
            var screenPos = Pointer.current.position.ReadValue();
            screenPos.y = Screen.height - screenPos.y;
            return RuntimePanelUtils.ScreenToPanel(appRoot.panel, screenPos);
        }

        public Vector2 GetUIPositionFromScreenPosition(Vector2 screenPos)
        {
            screenPos.y = Screen.height - screenPos.y;
            return RuntimePanelUtils.ScreenToPanel(appRoot.panel, screenPos);
        }

        public bool IsPointerOverUI()
        {
            return IsPointerOverUI(out _);
        }
        
        public bool IsPointerOverUI(out VisualElement picked)
        {
            Vector2 panelPosition = GetPanelClickPosition();
            picked = appRoot.panel.Pick(panelPosition);
            return picked != null;
        }
    }
}
