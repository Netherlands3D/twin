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
        private const string DashboardOpenClass = "app--dashboard-open";

        [SerializeField] private Configuration configuration;
        [SerializeField] private Functionality dashboardFunctionality;

        private InputService input;
        private InputAction toggleDashboardAction;
        private VisualElement root;
        private DashboardHUD dashboardOverlay;
        private ToolbarNavigation toolbarNavigation;
        private Button closeButton;
        private VisualElement previousFocus;

        private void Start()
        {
            root = GetComponent<AppRootBehaviour>().Root;
            dashboardOverlay = root.Q<DashboardHUD>("DashboardOverlay");
            toolbarNavigation = root.Q<ToolbarNavigation>();
            closeButton = root.Q<Button>("CloseDashboard");
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
            if (input.IsInputFieldSelected()) return;

            // The WebGL test override changes permission and Dashboarding together for this session only.
            var enable = !dashboardFunctionality.IsEnabled;
            configuration.AllowHiddenFeatures = enable;
            dashboardFunctionality.IsEnabled = enable;
        }

        private void OpenDashboard()
        {
            if (!isActiveAndEnabled || !dashboardFunctionality.IsEnabled || root.ClassListContains(DashboardOpenClass)) return;

            previousFocus = root.focusController?.focusedElement as VisualElement;
            root.AddToClassList(DashboardOpenClass);
            dashboardOverlay.StartDataBinding();
            // Move focus after the dashboard has become visible.
            closeButton.schedule.Execute(() =>
            {
                if (root.ClassListContains(DashboardOpenClass))
                    closeButton.Focus();
            });
        }

        private void CloseDashboard()
        {
            if (root == null || !root.ClassListContains(DashboardOpenClass)) return;

            root.RemoveFromClassList(DashboardOpenClass);
            dashboardOverlay.StopDataBinding();
            var elementToFocus = previousFocus;
            previousFocus = null;
            if (elementToFocus?.panel == null) return;

            // Restore focus after the viewer has become visible again.
            root.schedule.Execute(() =>
            {
                if (!root.ClassListContains(DashboardOpenClass) && elementToFocus.panel == root.panel)
                    elementToFocus.Focus();
            });
        }
    }
}
