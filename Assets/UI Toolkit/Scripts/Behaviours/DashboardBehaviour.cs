using Netherlands3D.Services;
using Netherlands3D.Twin.Configuration;
using Netherlands3D.Twin.Functionalities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Netherlands3D.UI_Toolkit.Scripts.Behaviours
{
    /// <summary>
    /// Controls Dashboarding activation, the temporary test override and the placeholder dashboard view.
    /// </summary>
    [RequireComponent(typeof(AppRootBehaviour))]
    public class DashboardBehaviour : MonoBehaviour
    {
        [SerializeField] private Configuration configuration;
        [SerializeField] private Functionality dashboardFunctionality;

        private AppRootBehaviour appRoot;
        private InputService input;
        private Button closeButton;
        private VisualElement previousFocus;
        private bool isOpen;

        private void Start()
        {
            appRoot = GetComponent<AppRootBehaviour>();
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
            input.ToggleDashboardAction.performed += OnToggleDashboard;
            dashboardFunctionality.OnEnableFunctionality.AddListener(OpenDashboard);
            dashboardFunctionality.OnDisableFunctionality.AddListener(CloseDashboard);
            closeButton.clicked += CloseDashboard;

            if (dashboardFunctionality.IsEnabled)
                OpenDashboard();
        }

        private void OnDisable()
        {
            if (input == null) return;

            input.ToggleDashboardAction.performed -= OnToggleDashboard;
            dashboardFunctionality.OnEnableFunctionality.RemoveListener(OpenDashboard);
            dashboardFunctionality.OnDisableFunctionality.RemoveListener(CloseDashboard);
            closeButton.clicked -= CloseDashboard;
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
            if (!dashboardFunctionality.IsEnabled || isOpen) return;

            previousFocus = appRoot.Root.focusController?.focusedElement as VisualElement;
            isOpen = true;
            appRoot.SetDashboardVisible(true);
            // Focus can only move after the newly visible UI has been laid out.
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
            var elementToFocus = previousFocus;
            previousFocus = null;
            if (elementToFocus?.panel == null) return;

            appRoot.Root.schedule.Execute(() =>
            {
                if (!isOpen && elementToFocus.panel == appRoot.Root.panel)
                    elementToFocus.Focus();
            });
        }
    }
}
