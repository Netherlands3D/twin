using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Services;
using Netherlands3D.Twin.Configuration;
using Netherlands3D.Twin.DataSets;
using Netherlands3D.Twin.Functionalities;
using Netherlands3D.Twin.Projects;
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
        internal DashboardObjectBinding ObjectBinding { get; private set; }
        private DashboardMapStyling mapStyling;
        private readonly Dictionary<string, Color> mapColors = new(StringComparer.OrdinalIgnoreCase);
        private VisualElement paletteSource;
        private DashboardMapLayer selectedMapLayer;

        private void Start()
        {
            appRoot = GetComponent<AppRootBehaviour>();
            dashboardOverlay = appRoot.Root.Q<DashboardHUD>("DashboardOverlay");
            toolbarNavigation = appRoot.Root.Q<ToolbarNavigation>();
            closeButton = appRoot.Root.Q<Button>("CloseDashboard");
            paletteSource = appRoot.Root.Q("HousingBlockChart");
            input = ServiceLocator.GetService<InputService>();
            Subscribe();
        }

        private void OnEnable()
        {
            if (input != null)
                Subscribe();
        }

        private void LateUpdate()
        {
            // Imported meshes receive their normal NL3D styling after the binding event.
            mapStyling?.ApplyPendingMeshColors();
        }

        private void Subscribe()
        {
            if (toggleDashboardAction != null) return;

            toggleDashboardAction = input.ToggleDashboardAction;
            toggleDashboardAction.performed += OnToggleDashboard;
            toolbarNavigation.OnDashboardClicked += OpenDashboard;
            closeButton.clicked += CloseDashboard;
            dashboardFunctionality.OnDisableFunctionality.AddListener(CloseDashboard);
            ObjectBinding = new DashboardObjectBinding(Netherlands3D.Twin.App.Layers, ProjectDataHandler.Instance, dashboardFunctionality);
            mapStyling = new DashboardMapStyling(ObjectBinding);
            ObjectBinding.Changed += OnMapDataChanged;
            paletteSource.RegisterCallback<CustomStyleResolvedEvent>(OnMapStylesResolved);
            ReadMapColors(paletteSource.customStyle);
            OnMapDataChanged();
        }

        private void OnDisable()
        {
            paletteSource?.UnregisterCallback<CustomStyleResolvedEvent>(OnMapStylesResolved);
            mapStyling?.Dispose();
            mapStyling = null;
            if (ObjectBinding != null)
                ObjectBinding.Changed -= OnMapDataChanged;
            ObjectBinding?.Dispose();
            ObjectBinding = null;
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

        private void OnMapStylesResolved(CustomStyleResolvedEvent evt)
        {
            if (ReadMapColors(evt.customStyle))
                OnMapDataChanged();
        }

        private bool ReadMapColors(ICustomStyle styles) => DashboardColors.Read(styles, mapColors);

        private void OnMapDataChanged()
        {
            foreach (var key in mapColors.Keys.Where(key => key.StartsWith("segment:", StringComparison.Ordinal)).ToArray())
                mapColors.Remove(key);

            if (ObjectBinding?.HasData == true)
            {
                foreach (var segment in ObjectBinding.HousingSegments)
                    mapColors[DashboardColors.SegmentKey(segment)] = DashboardColors.GetColor(segment, mapColors);
            }

            RefreshMapColors();
        }

        private void OnMapLayerChanged(int index)
        {
            selectedMapLayer = (DashboardMapLayer)Math.Max(0, index);
            RefreshMapColors();
        }

        private void RefreshMapColors() => mapStyling?.Update(selectedMapLayer,
            isOpen && ObjectBinding?.HasData == true && DashboardColors.HasMapColors(mapColors) ? mapColors : null);

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
            RefreshMapColors();
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
            RefreshMapColors();
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
