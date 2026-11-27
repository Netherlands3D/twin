using Netherlands3D.Services;
using Netherlands3D.Twin.Configuration;
using Netherlands3D.Twin.Functionalities;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Netherlands3D.UI_Toolkit.Scripts.Behaviours
{
    /// <summary>
    /// Controls Dashboarding activation, the temporary test override and the dashboard view.
    /// </summary>
    [RequireComponent(typeof(AppRootBehaviour))]
    public class DashboardBehaviour : MonoBehaviour
    {
        [SerializeField] private Configuration configuration;
        [SerializeField] private Functionality dashboardFunctionality;

        private InputService input;
        private InputAction toggleDashboardAction;
        private AppRootBehaviour appRoot;
        private DashboardHUD dashboardOverlay;
        private ToolbarNavigation toolbarNavigation;
        private Button closeButton;
        private VisualElement previousFocus;
        private bool isOpen;

        private void Start()
        {
            appRoot = GetComponent<AppRootBehaviour>();
            dashboardOverlay = appRoot.Root.Q<DashboardHUD>("DashboardOverlay");
            toolbarNavigation = appRoot.Root.Q<ToolbarNavigation>();
            closeButton = appRoot.Root.Q<Button>("CloseDashboard");
            input = ServiceLocator.GetService<InputService>();
            Subscribe();
        }

        private void OnEnable()
        {
            if (input != null)
                Subscribe();
        }

        private void Subscribe()
        {
            if (toggleDashboardAction != null) return;

            toggleDashboardAction = input.ToggleDashboardAction;
            toggleDashboardAction.performed += OnToggleDashboard;
            toolbarNavigation.OnDashboardClicked += OpenDashboard;
            closeButton.clicked += CloseDashboard;
            dashboardFunctionality.OnDisableFunctionality.AddListener(CloseDashboard);
        }

        private void OnDisable()
        {
            if (toggleDashboardAction != null)
            {
                toggleDashboardAction.performed -= OnToggleDashboard;
                toggleDashboardAction = null;
            }

            if (toolbarNavigation != null)
                toolbarNavigation.OnDashboardClicked -= OpenDashboard;
            if (closeButton != null)
                closeButton.clicked -= CloseDashboard;
            if (dashboardFunctionality != null)
                dashboardFunctionality.OnDisableFunctionality.RemoveListener(CloseDashboard);
            CloseDashboard();
        }

        private void OnToggleDashboard(InputAction.CallbackContext _)
        {
            // The WebGL test override changes permission and Dashboarding together for this session only.
            var enable = !dashboardFunctionality.IsEnabled;
            dashboardFunctionality.IsEnabled = enable;
        }

        private void OpenDashboard()
        {
            if (!isActiveAndEnabled || !dashboardFunctionality.IsEnabled || isOpen) return;

            previousFocus = appRoot.Root.focusController?.focusedElement as VisualElement;
            isOpen = true;
            appRoot.SetDashboardVisible(true);
            dashboardOverlay.StartDataBinding();
            // Move focus after the dashboard has become visible.
            closeButton.schedule.Execute(() =>
            {
                if (isOpen)
                    closeButton.Focus();
            });
        }

        private void CloseDashboard()
        {
            if (!isOpen) return;

            isOpen = false;
            appRoot.SetDashboardVisible(false);
            dashboardOverlay.StopDataBinding();
            var elementToFocus = previousFocus;
            previousFocus = null;
            if (elementToFocus?.panel == null) return;

            // Restore focus after the viewer has become visible again.
            appRoot.Root.schedule.Execute(() =>
            {
                if (!isOpen && elementToFocus.panel == appRoot.Root.panel)
                    elementToFocus.Focus();
            });
        }
    }
}
