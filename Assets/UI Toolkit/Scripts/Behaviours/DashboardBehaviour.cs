using System;
using System.Collections.Generic;
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
        internal DashboardObjectBinding ObjectBinding { get; private set; }
        private DashboardMapStyling mapStyling;
        private readonly Dictionary<string, Color> mapColors = new(StringComparer.OrdinalIgnoreCase);
        private VisualElement paletteSource;
        private DashboardMapLayer selectedMapLayer;
        private VisualElement mapColorPanel;
        private VisualElement mapColorContent;
        private Label mapColorEmpty;
        private DropDown mapColorSelection;
        private VisualElement mapLegend;

        private void Start()
        {
            root = GetComponent<AppRootBehaviour>().Root;
            dashboardOverlay = root.Q<DashboardHUD>("DashboardOverlay");
            toolbarNavigation = root.Q<ToolbarNavigation>();
            closeButton = root.Q<Button>("CloseDashboard");
            paletteSource = root.Q("HousingBlockChart");
            mapColorPanel = root.Q("DashboardMapColors");
            mapColorContent = mapColorPanel.Q("MapColorContent");
            mapColorEmpty = mapColorPanel.Q<Label>("MapColorEmpty");
            mapColorSelection = mapColorPanel.Q<DropDown>("ColorMode");
            mapLegend = mapColorPanel.Q("ColorLegend");
            // The NL3D dropdown's text style is available after its UXML has been deserialized.
            mapColorSelection.SetValue(0);
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
            mapColorSelection.DropDownValueChanged.AddListener(OnMapLayerChanged);
            paletteSource.RegisterCallback<CustomStyleResolvedEvent>(OnMapStylesResolved);
            ReadMapColors(paletteSource.customStyle);
            OnMapDataChanged();
        }

        private void OnDisable()
        {
            mapColorSelection?.DropDownValueChanged.RemoveListener(OnMapLayerChanged);
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
            DashboardColors.SetSegmentColors(ObjectBinding?.HasData == true ? ObjectBinding.HousingSegments : Array.Empty<string>(), mapColors);
            DashboardColors.SetPhaseColors(ObjectBinding?.HasData == true ? ObjectBinding.AvailablePhases : Array.Empty<string>(), mapColors);

            RefreshMapColors();
            RefreshMapLegend();
        }

        private void OnMapLayerChanged(int index)
        {
            selectedMapLayer = (DashboardMapLayer)Math.Max(0, index);
            RefreshMapColors();
            RefreshMapLegend();
        }

        private void RefreshMapColors() => mapStyling?.Update(selectedMapLayer,
            ObjectBinding?.HasData == true && DashboardColors.HasMapColors(mapColors) ? mapColors : null);

        private void RefreshMapLegend()
        {
            mapLegend.Clear();
            var hasData = ObjectBinding?.HasData == true;
            var hasLinkedObjects = hasData && (ObjectBinding.Objects.Count > 0 || ObjectBinding.ObjObjects.Count > 0 || ObjectBinding.GlbObjects.Count > 0);
            mapColorEmpty.text = hasData ? "Geen 3D-objecten gekoppeld aan de dashboardgegevens." : "Geen gegevens beschikbaar";
            mapColorContent.EnableInClassList(UtilityClassConstants.HIDDEN, !hasLinkedObjects);
            mapColorEmpty.EnableInClassList(UtilityClassConstants.HIDDEN, hasLinkedObjects);
            if (!hasLinkedObjects || !DashboardColors.HasMapColors(mapColors))
                return;

            switch (selectedMapLayer)
            {
                case DashboardMapLayer.Base:
                    AddMapLegendEntry("base", "Basiskleur / geen data");
                    break;
                case DashboardMapLayer.Segment:
                    foreach (var segment in ObjectBinding.HousingSegments)
                    {
                        var key = DashboardColors.SegmentKey(segment);
                        AddMapLegendEntry(key, "Meest " + segment);
                    }
                    break;
                case DashboardMapLayer.Dwellings:
                    AddMapLegendEntry("low", "Minder dan 300 woningen");
                    AddMapLegendEntry("medium", "300 tot en met 599 woningen");
                    AddMapLegendEntry("high", "Vanaf 600 woningen");
                    break;
                case DashboardMapLayer.Phase:
                    foreach (var phase in ObjectBinding.AvailablePhases)
                        AddMapLegendEntry(DashboardColors.PhaseKey(phase), phase);
                    break;
            }
            if (selectedMapLayer != DashboardMapLayer.Base)
                AddMapLegendEntry("base", "Geen bruikbare data");
        }

        private void AddMapLegendEntry(string key, string text) => ChartUtility.AddLegendEntry(mapLegend, "dashboard-map-layers", text, mapColors[key]);

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
