using System;
using System.IO;
using Netherlands3D.Services;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Configuration;
using Netherlands3D.Twin.DataSets;
using Netherlands3D.Twin.Functionalities;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.UI.Components;
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
        private DashboardDataBinding dataBinding;
        private DashboardHeader dashboardHeader;
        private DashboardCsvPropertyData logoSource;
        private Texture2D logoTexture;
        private ToolbarNavigation toolbarNavigation;
        private Button closeButton;
        private VisualElement previousFocus;
        private bool isOpen;

        private void Start()
        {
            appRoot = GetComponent<AppRootBehaviour>();
            dashboardHeader = appRoot.Root.Q<DashboardHeader>();
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
            dataBinding = new DashboardDataBinding(App.Layers, ProjectDataHandler.Instance, dashboardFunctionality);
            dataBinding.Changed += RefreshProjectTitle;
            RefreshProjectTitle();
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
            if (dataBinding != null)
            {
                dataBinding.Changed -= RefreshProjectTitle;
                dataBinding.Dispose();
                dataBinding = null;
                SetLogoSource(null);
            }
            CloseDashboard();
        }

        private void RefreshProjectTitle()
        {
            dashboardHeader.SetProjectTitle(dataBinding.Data?.ProjectTitle);
            SetLogoSource(dataBinding.Data != null ? dataBinding.Source : null);
        }

        private void SetLogoSource(DashboardCsvPropertyData source)
        {
            if (logoSource == source)
                return;

            if (logoSource != null)
                logoSource.OnLogoFileChanged.RemoveListener(RefreshLogo);

            logoSource = source;
            if (logoSource != null)
                logoSource.OnLogoFileChanged.AddListener(RefreshLogo);

            RefreshLogo(logoSource?.LogoFile);
        }

        private void RefreshLogo(Uri logoFile)
        {
            dashboardHeader.SetLogo(null);
            if (logoTexture != null)
            {
                Destroy(logoTexture);
                logoTexture = null;
            }

            if (logoFile == null || logoFile.Scheme != "project")
                return;

            var texture = new Texture2D(2, 2);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(AssetUriFactory.GetLocalPath(logoFile)), true))
                {
                    Debug.LogWarning("Het dashboardlogo kan niet worden geladen. Kies een geldig PNG- of JPG-bestand.");
                    return;
                }

                logoTexture = texture;
                dashboardHeader.SetLogo(logoTexture);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or UnityException)
            {
                Debug.LogWarning($"Het dashboardlogo kan niet worden geladen ({exception.GetType().Name}).");
            }
            finally
            {
                if (logoTexture != texture)
                    Destroy(texture);
            }
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

            RefreshProjectTitle();
            previousFocus = appRoot.Root.focusController?.focusedElement as VisualElement;
            isOpen = true;
            appRoot.SetDashboardVisible(true);
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
