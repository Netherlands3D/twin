using System;
using Netherlands3D.UI.Components;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.Layers.UI.HierarchyInspector
{
    public class ScenarioManager : MonoBehaviour
    {
        private const string ScenarioPrefix = "Scenario:";

        private readonly List<LayerData> scenarios = new();
        private readonly HashSet<LayerData> subscribedLayers = new();

        private ToolbarScenario toolbar;
        private LayerData selectedScenario;

        private void OnEnable()
        {
            toolbar = App.UIRoot.Root.Q<ToolbarScenario>();

            if (toolbar == null)
            {
                Debug.LogError(
                    "ToolbarScenario is missing from the UI Toolkit hierarchy."
                );
                return;
            }

            toolbar.SelectionChanged += OnToolbarSelectionChanged;

            App.Layers.LayerAdded.AddListener(OnLayerAdded);
            App.Layers.LayerRemoved.AddListener(OnLayerRemoved);
            ProjectData.Current.OnDataChanged.AddListener(
                OnProjectDataChanged
            );

            RebuildScenarios();
        }

        private void OnDisable()
        {
            if (toolbar != null)
            {
                toolbar.SelectionChanged -=
                    OnToolbarSelectionChanged;
            }

            App.Layers.LayerAdded.RemoveListener(OnLayerAdded);
            App.Layers.LayerRemoved.RemoveListener(OnLayerRemoved);
            ProjectData.Current.OnDataChanged.RemoveListener(
                OnProjectDataChanged
            );

            UnsubscribeFromAllLayers();
        }

        private void OnLayerAdded(LayerData layer)
        {
            SubscribeToLayer(layer);
            if (layer.HasProperty<ScenarioPropertyData>())
                selectedScenario = layer;
            RebuildScenarios();
        }
        
        private void OnLayerRemoved(LayerData layer)
        {
            UnsubscribeFromLayer(layer);

            if (selectedScenario == layer)
                selectedScenario = null;

            RebuildScenarios();
        }

        private void OnProjectDataChanged(ProjectData _)
        {
            selectedScenario = null;
            RebuildScenarios();
        }

        private void OnLayerNameChanged(LayerData layer, string name)
        {
            if (!layer.HasProperty<FolderPropertyData>() || !TryGetScenarioName(name, out var scenarioName))
            {
                RebuildScenarios();
                return;
            }

            layer.Name = scenarioName;
            selectedScenario = layer;
            SetScenarioState(layer, true);
        }

        private void OnLayerPropertyChanged(LayerPropertyData _)
        {
            RebuildScenarios();
        }

        private static bool TryGetScenarioName(string name, out string scenarioName)
        {
            scenarioName = null;

            if (string.IsNullOrWhiteSpace(name) || !name.StartsWith(ScenarioPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            scenarioName = name.Substring(ScenarioPrefix.Length).Trim();

            if (string.IsNullOrWhiteSpace(scenarioName))
                scenarioName = "Nieuw scenario";

            return true;
        }

        private void RebuildScenarios()
        {
            if (toolbar == null)
                return;

            var layers = ProjectData.Current.RootLayer
                .GetFlatHierarchy();

            RefreshLayerSubscriptions(layers);

            scenarios.Clear();
            scenarios.AddRange(
                layers.Where(
                    layer =>
                        layer.HasProperty<ScenarioPropertyData>()
                )
            );

            if (selectedScenario != null &&
                !scenarios.Contains(selectedScenario))
            {
                selectedScenario = null;
            }

            // Preserve existing behaviour: automatically select
            // the first scenario when scenarios exist.
            if (selectedScenario == null && scenarios.Count > 0)
                selectedScenario = scenarios[0];

            ApplyScenarioVisibility();

            var labels = scenarios
                .Select(scenario => GetScenarioLabel(scenario.Name))
                .ToList();

            toolbar.SetScenarios(
                labels,
                GetSelectedScenarioIndex()
            );
        }

        private void OnToolbarSelectionChanged(int? selectedIndex)
        {
            if (!selectedIndex.HasValue)
            {
                if (selectedScenario != null)
                {
                    SetScenarioActive(
                        selectedScenario,
                        false
                    );
                }

                selectedScenario = null;
                return;
            }

            if (selectedIndex.Value < 0 ||
                selectedIndex.Value >= scenarios.Count)
            {
                return;
            }

            var newScenario = scenarios[selectedIndex.Value];

            if (selectedScenario == newScenario)
                return;

            if (selectedScenario != null)
            {
                SetScenarioActive(
                    selectedScenario,
                    false
                );
            }

            selectedScenario = newScenario;
            SetScenarioActive(selectedScenario, true);
        }

        private void ApplyScenarioVisibility()
        {
            foreach (var scenario in scenarios)
            {
                SetScenarioActive(
                    scenario,
                    scenario == selectedScenario
                );
            }
        }

        private static void SetScenarioActive(LayerData scenario, bool active)
        {
            scenario.ActiveSelf = active;

            if (active)
            {
                if (!scenario.IsSelected)
                    scenario.SelectLayer();
            }
            else
            {
                if (scenario.IsSelected)
                    scenario.DeselectLayer();
            }
        }

        private int? GetSelectedScenarioIndex()
        {
            if (selectedScenario == null)
                return null;

            var index = scenarios.IndexOf(selectedScenario);
            return index >= 0 ? index : null;
        }

        public static void SetScenarioState(LayerData layer, bool isScenario)
        {
            if (isScenario)
            {
                if (layer.HasProperty<ScenarioPropertyData>())
                    return;

                layer.SetProperty(new ScenarioPropertyData());

                var folderProperty = layer.GetProperty<FolderPropertyData>();

                if (folderProperty != null)
                    layer.RemoveProperty(folderProperty);

                return;
            }

            if (layer.HasProperty<FolderPropertyData>())
                return;

            layer.SetProperty(new FolderPropertyData());

            var scenarioProperty = layer.GetProperty<ScenarioPropertyData>();

            if (scenarioProperty != null)
                layer.RemoveProperty(scenarioProperty);
        }

        private static bool IsScenarioName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) &&
                   name.StartsWith(
                       ScenarioPrefix,
                       StringComparison.OrdinalIgnoreCase
                   );
        }

        private static string GetScenarioLabel(string name)
        {
            if (!IsScenarioName(name))
                return name;

            return name
                .Substring(ScenarioPrefix.Length)
                .Trim();
        }

        private void RefreshLayerSubscriptions(
            IReadOnlyCollection<LayerData> layers
        )
        {
            var removedLayers = subscribedLayers
                .Where(layer => !layers.Contains(layer))
                .ToList();

            foreach (var layer in removedLayers)
                UnsubscribeFromLayer(layer);

            foreach (var layer in layers)
                SubscribeToLayer(layer);
        }

        private void SubscribeToLayer(LayerData layer)
        {
            if (!subscribedLayers.Add(layer))
                return;

            layer.NameChanged.AddListener(OnLayerNameChanged);
            layer.PropertySet.AddListener(OnLayerPropertyChanged);
            layer.PropertyRemoved.AddListener(OnLayerPropertyChanged);
        }

        private void UnsubscribeFromLayer(LayerData layer)
        {
            if (!subscribedLayers.Remove(layer))
                return;

            layer.NameChanged.RemoveListener(OnLayerNameChanged);
            layer.PropertySet.RemoveListener(OnLayerPropertyChanged);
            layer.PropertyRemoved.RemoveListener(OnLayerPropertyChanged);
        }

        private void UnsubscribeFromAllLayers()
        {
            foreach (var layer in subscribedLayers)
            {
                layer.NameChanged.RemoveListener(OnLayerNameChanged);
                layer.PropertySet.RemoveListener(OnLayerPropertyChanged);
                layer.PropertyRemoved.RemoveListener(OnLayerPropertyChanged);
            }

            subscribedLayers.Clear();
        }
    }
}
