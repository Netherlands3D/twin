using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Projects;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.UI.HierarchyInspector
{
    public class ScenarioManager : MonoBehaviour
    {
        [Header("UI wiring")]
        [SerializeField] private RectTransform scenarioToggleContainer;
        [SerializeField] private Toggle scenarioTogglePrefab;

        private readonly List<Scenario> scenarios = new();
        private Scenario selectedScenario;

        private void Awake()
        {
            if (!scenarioToggleContainer || !scenarioTogglePrefab)
            {
                Debug.LogError("ScenarioToggleContainer or ScenarioTogglePrefab is NOT assigned in the inspector.");
            }
        }

        private void OnEnable()
        {
            ProjectData.Current.OnDataChanged.AddListener(OnProjectDataChanged);
            App.Layers.LayerAdded.AddListener(OnLayerAdded);
            App.Layers.LayerRemoved.AddListener(OnLayerDeleted);
        }

        private void OnDisable()
        {
            ProjectData.Current.OnDataChanged.RemoveListener(OnProjectDataChanged);
            App.Layers.LayerAdded.RemoveListener(OnLayerAdded);
            App.Layers.LayerRemoved.RemoveListener(OnLayerDeleted);
        }

        private void OnProjectDataChanged(ProjectData data)
        {
            RebuildScenarioUI();
        }

        private void OnLayerAdded(LayerData layer)
        {
            if (layer.PrefabIdentifier == ScenarioPreset.PrefabIdentifier)
            {
                AddScenario(layer);
                SetScenarioContainerEnabled(true);
                SelectScenario(layer);
            }
        }
        
        private void OnLayerDeleted(LayerData layer)
        {
            if (layer.PrefabIdentifier == ScenarioPreset.PrefabIdentifier)
            {
                RemoveScenario(layer);
                if(selectedScenario?.Layer == layer)
                    selectedScenario = null;
                if (scenarios.Count == 0)
                    SetScenarioContainerEnabled(false);
            }
        }

        private void AddScenario(LayerData layer)
        {
            var toggle = Instantiate(scenarioTogglePrefab, scenarioToggleContainer);
            Scenario scenario = toggle.GetComponent<Scenario>();
            scenario.SetLabel(layer.Name);
            scenario.SetLayer(layer);
            scenarios.Add(scenario);
            toggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                    SelectScenario(scenario.Layer);
            });
            
        }

        private void RemoveScenario(LayerData layer)
        {
            foreach (var scenario in scenarios)
            {
                if (scenario.Layer == layer)
                {
                    scenarios.Remove(scenario);
                    Destroy(scenario.gameObject);
                    return;
                }
            }
        }

        private void SelectScenario(LayerData layer)
        {
            foreach (var scenario in scenarios)
            {
                if (scenario.Layer == layer)
                {
                    ActivateScenario(scenario.Layer);
                    selectedScenario = scenario;
                    return;
                }
            }
        }

        private void SetScenarioContainerEnabled(bool enabled)
        {
            scenarioToggleContainer.gameObject.SetActive(enabled);
        }

        private void RebuildScenarioUI()
        {
            // Default: hide the scenario bar; only show when we actually have scenarios
            SetScenarioContainerEnabled(false);
 
            // Clear existing UI
            Scenario[] markers = scenarioToggleContainer.GetComponentsInChildren<Scenario>();
            foreach (Scenario marker in markers)
            {
                Destroy(marker.gameObject);
            }
            scenarios.Clear();
            // Find scenario folders in the whole layer tree
            List<LayerData> layers = ProjectData.Current.RootLayer.GetFlatHierarchy();
            foreach (LayerData layer in layers)
            {
                if (layer.PrefabIdentifier == ScenarioPreset.PrefabIdentifier)
                    AddScenario(layer);
            }
            if (!scenarios.Any())  return;
            
            SetScenarioContainerEnabled(true);
            SelectScenario(scenarios[0].Layer);
          
        }

        private void ActivateScenario(LayerData activeFolder)
        {
            foreach (var scenario in scenarios)
            {
                bool shouldBeOn = (scenario.Layer == activeFolder);
                scenario.Toggle.isOn = shouldBeOn;
                scenario.Layer.ActiveSelf = shouldBeOn;
            }
        }
    }
}
