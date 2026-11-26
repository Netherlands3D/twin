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
        private readonly HashSet<(MeshRenderer Renderer, int MaterialIndex)> selectedMeshObjects = new();
        private (MeshRenderer Renderer, int MaterialIndex)? pendingMeshSelection;
        private bool objectSelectionChanged;
        private InputAction toggleDashboardAction;
        private VisualElement root;
        private DashboardHUD dashboardOverlay;
        private DashboardAnnotation dashboardAnnotation;
        private DashboardHeader dashboardHeader;
        private ToolbarNavigation toolbarNavigation;
        private Button closeButton;
        private VisualElement previousFocus;
        internal DashboardObjectBinding ObjectBinding { get; private set; }
        internal CityJSONMapping SelectedObjectMapping { get; private set; }
        internal (MeshRenderer Renderer, int MaterialIndex)? SelectedMeshObject { get; private set; }
        internal DashboardObjectData.BlockData SelectedObjectBlock => GetObjectBlock(SelectedObjectMapping) ?? GetObjectBlock(SelectedMeshObject);
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
            dashboardAnnotation = dashboardOverlay.Q<DashboardAnnotation>("DashboardAnnotation");
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
            var mesh = pendingMeshSelection;
            pendingMeshSelection = null;
            var block = GetObjectBlock(mesh);
            if (block != null)
            {
                selectionService.Deselect();
                SelectObjectBlock(block.Block, meshPrimary: mesh);
            }
            // Imported meshes receive their normal NL3D styling after the binding event.
            mapStyling?.ApplyPendingMeshColors();
            // SelectionService deselects before selecting the next object; apply only the final toolbar state.
            if (objectSelectionChanged)
            {
                objectSelectionChanged = false;
                dashboardHeader.SelectBlock(SelectedObjectBlock?.Block);
                dashboardOverlay.SetBlock(dashboardHeader.SelectedBlock);
                dashboardAnnotation.SetData(SelectedObjectBlock);
            }
            UpdateAnnotationPosition();
        }

        private void Subscribe()
        {
            if (toggleDashboardAction != null) return;

            toggleDashboardAction = input.ToggleDashboardAction;
            toggleDashboardAction.performed += OnToggleDashboard;
            selectionService.SelectCityJsonMapping.AddListener(OnCityJsonSelected);
            selectionService.OnDeselect.AddListener(OnObjectDeselected);
            selectionService.AddSelectionPredicate(AllowNormalSelection);
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
                selectionService.RemoveSelectionPredicate(AllowNormalSelection);
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
            ObjectBinding?.SetObjPickingEnabled(root.ClassListContains(DashboardOpenClass));
            dashboardHeader.SetBlocks(ObjectBinding?.HasData == true ? ObjectBinding.BlockIdentifiers : Array.Empty<string>());
            dashboardOverlay.SetData(root.ClassListContains(DashboardOpenClass) ? ObjectBinding?.Data : null, dashboardHeader.SelectedBlock);
            var linkedBlocks = ObjectBinding?.HasData == true
                ? ObjectBinding.Objects.Values.Concat(ObjectBinding.ObjObjects.Values).Concat(ObjectBinding.GlbObjects.Values)
                    .Select(block => block.Block).ToHashSet(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            dashboardHeader.SetLinkedBlocks(linkedBlocks);
            dashboardOverlay.SetMissingGeometry(ObjectBinding?.HasData == true && ObjectBinding.BlockIdentifiers.Any(block => !linkedBlocks.Contains(block)));
            if (root.ClassListContains(DashboardOpenClass))
            {
                if (dashboardHeader.SelectedBlock != null)
                {
                    SelectObjectBlock(dashboardHeader.SelectedBlock);
                    if (SelectedObjectBlock == null)
                        objectSelectionChanged = false;
                }
                else if (selectedObjects.Count > 0 || selectedMeshObjects.Count > 0)
                    selectionService.Deselect();
            }
            dashboardAnnotation.SetData(SelectedObjectBlock);

            DashboardColors.SetSegmentColors(ObjectBinding?.HasData == true ? ObjectBinding.HousingSegments : Array.Empty<string>(), mapColors);
            DashboardColors.SetPhaseColors(ObjectBinding?.HasData == true ? ObjectBinding.AvailablePhases : Array.Empty<string>(), mapColors);
            dashboardAnnotation.SetPhaseColors(mapColors);

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

        private DashboardObjectData.BlockData GetObjectBlock((MeshRenderer Renderer, int MaterialIndex)? slot) =>
            slot.HasValue ? ObjectBinding?.GetMeshBlock(slot.Value.Renderer, slot.Value.MaterialIndex) : null;

        private bool AllowNormalSelection()
        {
            pendingMeshSelection = null;
            if (!root.ClassListContains(DashboardOpenClass) || Pointer.current == null || ObjectBinding?.HasData != true
                || ObjectBinding.GlbObjects.Count == 0 && ObjectBinding.ObjObjects.Count == 0)
                return true;

            var ray = Netherlands3D.Twin.App.Cameras.ActiveCamera.ScreenPointToRay(Pointer.current.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit, Mathf.Infinity))
                return true;

            var renderer = hit.collider.GetComponent<MeshRenderer>();
            var materialIndex = GetHitMaterialIndex(hit);
            if (ObjectBinding.GetMeshBlock(renderer, materialIndex) == null)
                return true;

            pendingMeshSelection = (renderer, materialIndex);
            return false;
        }

        private static int GetHitMaterialIndex(RaycastHit hit)
        {
            if (hit.collider is not MeshCollider collider || collider.sharedMesh == null || hit.triangleIndex < 0)
                return -1;

            var mesh = collider.sharedMesh;
            // MeshCollider triangle indices address the concatenated triangle submeshes.
            long triangle = hit.triangleIndex;
            for (var i = 0; i < mesh.subMeshCount; i++)
            {
                var triangles = mesh.GetIndexCount(i) / 3;
                if (triangle < triangles)
                    return i;
                triangle -= triangles;
            }
            return -1;
        }

        private void OnBlockClicked()
        {
            selectionService.Deselect();
            var block = dashboardHeader.SelectedBlock;
            if (block != null && ObjectBinding?.HasData == true)
                SelectObjectBlock(block);

            // Preserve CSV-only block choices when no selectable geometry is available.
            if (SelectedObjectBlock == null)
                objectSelectionChanged = false;
            dashboardOverlay.SetBlock(block);
        }

        private void SelectObjectBlock(string block, CityJSONMapping primary = null, (MeshRenderer Renderer, int MaterialIndex)? meshPrimary = null)
        {
            ClearObjectSelection(clearAnnotation: false);
            foreach (var mapping in ObjectBinding.GetCityJsonMappings(block))
            {
                mapping.Select();
                selectedObjects.Add(mapping);
            }
            if (primary != null && selectedObjects.Add(primary))
                primary.Select();

            selectedMeshObjects.UnionWith(ObjectBinding.GetMeshSlots(block));
            SelectedObjectMapping = primary != null && selectedObjects.Contains(primary) ? primary : selectedObjects.FirstOrDefault();
            SelectedMeshObject = meshPrimary.HasValue && selectedMeshObjects.Contains(meshPrimary.Value) ? meshPrimary
                : selectedMeshObjects.Count > 0 ? selectedMeshObjects.First() : null;
            mapStyling.SelectBlock(selectedMeshObjects.Count > 0 ? block : null);
            objectSelectionChanged = true;
        }

        private void ClearObjectSelection(bool clearAnnotation = true)
        {
            pendingMeshSelection = null;
            if (selectedObjects.Count == 0 && selectedMeshObjects.Count == 0)
                return;

            foreach (var mapping in selectedObjects)
            {
                if (mapping.Feature != null)
                    mapping.Deselect();
            }
            selectedObjects.Clear();
            selectedMeshObjects.Clear();
            SelectedObjectMapping = null;
            SelectedMeshObject = null;
            if (clearAnnotation)
                dashboardAnnotation.SetData(null);
            mapStyling?.SelectBlock(null);
            objectSelectionChanged = true;
        }

        private void UpdateAnnotationPosition()
        {
            if (!root.ClassListContains(DashboardOpenClass) || SelectedObjectBlock == null)
            {
                dashboardAnnotation.SetPosition(default, false);
                return;
            }

            var bounds = new Bounds();
            var hasBounds = false;
            foreach (var mapping in selectedObjects)
            {
                if (GetObjectBlock(mapping) == null || mapping.Visualizer == null)
                    continue;

                var renderer = mapping.Visualizer.GetComponent<MeshRenderer>();
                if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy)
                    IncludeBounds(renderer.bounds);
            }
            foreach (var (renderer, materialIndex) in selectedMeshObjects)
            {
                if (GetObjectBlock((renderer, materialIndex)) == null)
                    continue;

                var filter = renderer != null ? renderer.GetComponent<MeshFilter>() : null;
                if (filter == null || filter.sharedMesh == null || materialIndex < 0 || materialIndex >= filter.sharedMesh.subMeshCount)
                    continue;

                var localBounds = filter.sharedMesh.GetSubMesh(materialIndex).bounds;
                if (localBounds.size.sqrMagnitude == 0f)
                    IncludeBounds(renderer.bounds);
                else
                {
                    var worldBounds = new Bounds(renderer.transform.TransformPoint(localBounds.center), Vector3.zero);
                    for (var x = -1; x <= 1; x += 2)
                        for (var y = -1; y <= 1; y += 2)
                            for (var z = -1; z <= 1; z += 2)
                                worldBounds.Encapsulate(renderer.transform.TransformPoint(localBounds.center + Vector3.Scale(localBounds.extents, new Vector3(x, y, z))));
                    IncludeBounds(worldBounds);
                }
            }
            if (!hasBounds)
            {
                dashboardAnnotation.SetPosition(default, false);
                return;
            }

            var camera = Netherlands3D.Twin.App.Cameras.ActiveCamera;
            var screenPosition = camera.WorldToScreenPoint(bounds.center);
            dashboardAnnotation.SetPosition(screenPosition.z > 0f ? Netherlands3D.Twin.App.UIRoot.GetUIPositionFromScreenPosition(screenPosition) : default, screenPosition.z > 0f);

            void IncludeBounds(Bounds geometryBounds)
            {
                if (hasBounds)
                    bounds.Encapsulate(geometryBounds);
                else
                {
                    bounds = geometryBounds;
                    hasBounds = true;
                }
            }
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
            ObjectBinding.SetObjPickingEnabled(true);
            if (transformHandles != null)
            {
                restoreTransformHandles = transformHandles.gameObject.activeSelf;
                transformHandles.gameObject.SetActive(false);
            }
            dashboardOverlay.SetData(ObjectBinding.Data, dashboardHeader.SelectedBlock);
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
            ObjectBinding?.SetObjPickingEnabled(false);
            selectionService.Deselect();
            ClearObjectSelection();
            dashboardHeader.SelectBlock(null);
            if (transformHandles != null)
                transformHandles.gameObject.SetActive(restoreTransformHandles && transformHandles.Target != null);
            restoreTransformHandles = false;
            objectSelectionChanged = false;
            dashboardOverlay.SetData(null);
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
