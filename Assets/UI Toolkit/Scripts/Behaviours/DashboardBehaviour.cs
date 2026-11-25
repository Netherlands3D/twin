using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Services;
using Netherlands3D.Twin.Configuration;
using Netherlands3D.Twin.DataSets;
using Netherlands3D.Twin.Functionalities;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.UI;
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
        private SelectionService selectionService;
        private TransformHandleInterfaceToggle transformHandles;
        private bool restoreTransformHandles;
        private readonly HashSet<CityJSONMapping> selectedObjects = new();
        private bool objectSelectionChanged;
        private InputAction toggleDashboardAction;
        private VisualElement root;
        private DashboardHUD dashboardOverlay;
        private DashboardHeader dashboardHeader;
        private ToolbarNavigation toolbarNavigation;
        private Button closeButton;
        private VisualElement previousFocus;
        internal DashboardObjectBinding ObjectBinding { get; private set; }
        internal CityJSONMapping SelectedObjectMapping { get; private set; }
        internal DashboardObjectData.BlockData SelectedObjectBlock => GetObjectBlock(SelectedObjectMapping);
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
            dashboardHeader = dashboardOverlay.Q<DashboardHeader>("DashboardHeader");
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
            selectionService = ServiceLocator.GetService<SelectionService>();
            transformHandles = ServiceLocator.GetService<TransformHandleInterfaceToggle>();
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
            // SelectionService deselects before selecting the next object; apply only the final toolbar state.
            if (objectSelectionChanged)
            {
                objectSelectionChanged = false;
                dashboardHeader.SelectBlock(SelectedObjectBlock?.Block);
            }
        }

        private void Subscribe()
        {
            if (toggleDashboardAction != null) return;

            toggleDashboardAction = input.ToggleDashboardAction;
            toggleDashboardAction.performed += OnToggleDashboard;
            selectionService.SelectCityJsonMapping.AddListener(OnCityJsonSelected);
            selectionService.OnDeselect.AddListener(OnObjectDeselected);
            transformHandles?.SetTarget.AddListener(OnTransformTargetChanged);
            dashboardHeader.BlockClicked += OnBlockClicked;
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
            transformHandles?.SetTarget.RemoveListener(OnTransformTargetChanged);
            if (dashboardHeader != null)
                dashboardHeader.BlockClicked -= OnBlockClicked;
            if (selectionService != null)
            {
                selectionService.SelectCityJsonMapping.RemoveListener(OnCityJsonSelected);
                selectionService.OnDeselect.RemoveListener(OnObjectDeselected);
            }
            ClearObjectSelection();
            objectSelectionChanged = false;
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
            dashboardHeader.SetBlocks(ObjectBinding?.HasData == true ? ObjectBinding.BlockIdentifiers : Array.Empty<string>());
            var linkedBlocks = ObjectBinding?.HasData == true
                ? ObjectBinding.Objects.Values.Concat(ObjectBinding.ObjObjects.Values).Concat(ObjectBinding.GlbObjects.Values)
                    .Select(block => block.Block).ToHashSet(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            dashboardHeader.SetLinkedBlocks(linkedBlocks);
            dashboardOverlay.SetMissingGeometry(ObjectBinding?.HasData == true && ObjectBinding.BlockIdentifiers.Any(block => !linkedBlocks.Contains(block)));
            if (selectedObjects.Any(mapping => GetObjectBlock(mapping) == null))
                selectionService.Deselect();

            DashboardColors.SetSegmentColors(ObjectBinding?.HasData == true ? ObjectBinding.HousingSegments : Array.Empty<string>(), mapColors);
            DashboardColors.SetPhaseColors(ObjectBinding?.HasData == true ? ObjectBinding.AvailablePhases : Array.Empty<string>(), mapColors);

            RefreshMapColors();
            RefreshMapLegend();
        }

        private DashboardObjectData.BlockData GetObjectBlock(CityJSONMapping mapping)
        {
            if (mapping?.Feature == null || mapping.VisualisationParent == null || mapping.LayerData.IsDisposed || !mapping.LayerData.ActiveInHierarchy || ObjectBinding?.HasData != true)
                return null;

            return ObjectBinding.Objects.TryGetValue(mapping.Feature, out var block) ? block : null;
        }

        private void OnCityJsonSelected(CityJSONMapping mapping)
        {
            if (!root.ClassListContains(DashboardOpenClass))
                return;

            var block = GetObjectBlock(mapping);
            if (block != null)
                SelectObjectBlock(block.Block, mapping);
            else
                ClearObjectSelection();
        }

        private void OnObjectDeselected() => ClearObjectSelection();

        private void OnBlockClicked()
        {
            selectionService.Deselect();
            var block = dashboardHeader.SelectedBlock;
            if (block != null && ObjectBinding?.HasData == true)
                SelectObjectBlock(block);

            // Preserve CSV-only block choices when no selectable geometry is available.
            if (SelectedObjectMapping == null)
                objectSelectionChanged = false;
        }

        private void SelectObjectBlock(string block, CityJSONMapping primary = null)
        {
            ClearObjectSelection();
            foreach (var mapping in ObjectBinding.GetCityJsonMappings(block))
            {
                mapping.Select();
                selectedObjects.Add(mapping);
            }
            if (primary != null && selectedObjects.Add(primary))
                primary.Select();

            foreach (var layer in selectedObjects.Select(mapping => mapping.LayerData).Distinct())
                layer.LayerActiveInHierarchyChanged.AddListener(OnSelectedObjectLayerActiveChanged);
            SelectedObjectMapping = primary != null && selectedObjects.Contains(primary) ? primary : selectedObjects.FirstOrDefault();
            objectSelectionChanged = true;
        }

        private void OnSelectedObjectLayerActiveChanged(bool active)
        {
            if (!active)
                selectionService.Deselect();
        }

        private void ClearObjectSelection()
        {
            if (selectedObjects.Count == 0)
                return;

            foreach (var layer in selectedObjects.Select(mapping => mapping.LayerData).Distinct())
                layer.LayerActiveInHierarchyChanged.RemoveListener(OnSelectedObjectLayerActiveChanged);
            foreach (var mapping in selectedObjects)
            {
                if (mapping.Feature != null)
                    mapping.Deselect();
            }
            selectedObjects.Clear();
            SelectedObjectMapping = null;
            objectSelectionChanged = true;
        }

        private void OnTransformTargetChanged(GameObject target)
        {
            if (!root.ClassListContains(DashboardOpenClass))
                return;

            restoreTransformHandles = target != null;
            transformHandles.gameObject.SetActive(false);
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
            if (transformHandles != null)
            {
                restoreTransformHandles = transformHandles.gameObject.activeSelf;
                transformHandles.gameObject.SetActive(false);
            }
            dashboardOverlay.StartDataBinding();
            foreach (var mapping in selectionService.SelectedMappings.Values)
            {
                if (mapping is not CityJSONMapping cityJson)
                    continue;

                var block = GetObjectBlock(cityJson);
                if (block == null)
                    continue;

                SelectObjectBlock(block.Block, cityJson);
                break;
            }
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
            selectionService.Deselect();
            ClearObjectSelection();
            if (transformHandles != null)
                transformHandles.gameObject.SetActive(restoreTransformHandles && transformHandles.Target != null);
            restoreTransformHandles = false;
            objectSelectionChanged = false;
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
