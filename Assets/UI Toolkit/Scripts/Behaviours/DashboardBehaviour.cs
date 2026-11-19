using Netherlands3D.Services;
using Netherlands3D.Twin.Configuration;
using Netherlands3D.Twin.Functionalities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Netherlands3D.UI_Toolkit.Scripts.Behaviours
{
    /// <summary>
    /// Controls Dashboarding activation and the temporary test override.
    /// </summary>
    [RequireComponent(typeof(AppRootBehaviour))]
    public class DashboardBehaviour : MonoBehaviour
    {
        [SerializeField] private Configuration configuration;
        [SerializeField] private Functionality dashboardFunctionality;

        private InputService input;

        private void Start()
        {
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
        }

        private void OnDisable()
        {
            if (input == null) return;

            input.ToggleDashboardAction.performed -= OnToggleDashboard;
        }

        private void OnToggleDashboard(InputAction.CallbackContext _)
        {
            if (input.IsInputFieldSelected()) return;

            // The WebGL test override changes permission and Dashboarding together for this session only.
            var enable = !dashboardFunctionality.IsEnabled;
            configuration.AllowHiddenFeatures = enable;
            dashboardFunctionality.IsEnabled = enable;
        }
    }
}
