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

        private readonly List<ScenarioToggleMarker> scenarios = new();

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
            }
        }
        
        private void OnLayerDeleted(LayerData layer)
        {
            if (layer.PrefabIdentifier == ScenarioPreset.PrefabIdentifier)
            {
                RemoveScenario(layer);
                if (scenarios.Count == 0)
                    SetScenarioContainerEnabled(false);
            }
        }

        private void AddScenario(LayerData layer)
        {
            var toggle = Instantiate(scenarioTogglePrefab, scenarioToggleContainer);
            ScenarioToggleMarker marker = toggle.GetComponent<ScenarioToggleMarker>();
            marker.SetLabel(layer.Name);
            marker.SetScenario(layer);
            scenarios.Add(marker);
            toggle.onValueChanged.AddListener(isOn =>
            {
                if (!isOn)
                {
                    Debug.Log("  Ignoring OFF event (we treat toggles like radio buttons).");
                    return;
                }
                ActivateScenario(marker.Scenario);
            });
            
        }

        private void RemoveScenario(LayerData layer)
        {
            foreach (var scenario in scenarios)
            {
                if (scenario.Scenario == layer)
                {
                    scenarios.Remove(scenario);
                    Destroy(scenario.gameObject);
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
            ScenarioToggleMarker[] markers = scenarioToggleContainer.GetComponentsInChildren<ScenarioToggleMarker>();
            foreach (ScenarioToggleMarker marker in markers)
            {
                Destroy(marker.gameObject);
            }
            scenarios.Clear();
            // Find scenario folders in the whole layer tree
            List<LayerData> scenarioFolders = new();
            List<LayerData> layers = ProjectData.Current.RootLayer.GetFlatHierarchy();
            foreach (LayerData layer in layers)
            {
                if (layer.PrefabIdentifier == ScenarioPreset.PrefabIdentifier)
                    AddScenario(layer);
            }
            if (!scenarios.Any())  return;
            
            SetScenarioContainerEnabled(true);
            ActivateScenario(scenarioFolders[0]);
          
        }

        private void ActivateScenario(LayerData activeFolder)
        {
            foreach (var scenario in scenarios)
            {
                bool shouldBeOn = (scenario.Scenario == activeFolder);
                scenario.Toggle.isOn = shouldBeOn;
                scenario.Scenario.ActiveSelf = shouldBeOn;
            }
        }
        
    }
}
