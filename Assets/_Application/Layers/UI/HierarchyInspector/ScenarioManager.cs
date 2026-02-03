using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Services;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // <- for TMP_Text

namespace Netherlands3D.Twin.Layers.UI.HierarchyInspector
{
    public class ScenarioManager : MonoBehaviour
    {
        [Header("UI wiring")]
        [SerializeField] private RectTransform scenarioToggleContainer;
        [SerializeField] private Toggle scenarioTogglePrefab;

        [Header("Debug")]
        [SerializeField] private bool debugLogging = false;

        private readonly List<ScenarioToggleMarker> scenarios = new();
        private bool suppressToggleEvents;

        private void Awake()
        {
            if (!scenarioToggleContainer || !scenarioTogglePrefab)
            {
                Log("ScenarioToggleContainer or ScenarioTogglePrefab is NOT assigned in the inspector.");
            }
        }

        private void Log(string msg)
        {
            if (debugLogging)
                Debug.Log($"[ScenarioManager] {msg}", this);
        }

        private void OnEnable()
        {
            Log("OnEnable called.");

            if (ProjectData.Current == null)
            {
                Log("ProjectData.Current is NULL in OnEnable. Waiting for project to be loaded.");
                // container off if no project
                if (scenarioToggleContainer) scenarioToggleContainer.gameObject.SetActive(false);
                return;
            }

            Log("ProjectData.Current is available in OnEnable.");

            RebuildScenarioUI();

            ProjectData.Current.OnDataChanged.AddListener(OnProjectDataChanged);
            App.Layers.LayerAdded.AddListener(OnLayerAdded);
            App.Layers.LayerRemoved.AddListener(OnLayerDeleted);
        }

        private void OnDisable()
        {
            Log("OnDisable called.");

            if (ProjectData.Current == null)
            {
                Log("ProjectData.Current is NULL in OnDisable. Nothing to unsubscribe.");
                return;
            }

            ProjectData.Current.OnDataChanged.RemoveListener(OnProjectDataChanged);
            App.Layers.LayerAdded.RemoveListener(OnLayerAdded);
            App.Layers.LayerRemoved.RemoveListener(OnLayerDeleted);

            Log("Unsubscribed from ProjectData events.");
        }

        private void OnProjectDataChanged(ProjectData data)
        {
            Log("OnProjectDataChanged fired. Rebuilding Scenario UI.");
            RebuildScenarioUI();
        }

        private void OnLayerAdded(LayerData layer)
        {
            Log($"OnLayerAdded: {layer?.Name ?? "(null)"} (type: {layer?.GetType().Name ?? "null"})");
            // Renames trigger OnDataChanged via LayerData.Name, so we don't need to do anything else here
        }

        private void OnLayerDeleted(LayerData layer)
        {
            Log($"OnLayerDeleted: {layer?.Name ?? "(null)"} (type: {layer?.GetType().Name ?? "null"})");
        }

        private void RebuildScenarioUI()
        {
            Log("RebuildScenarioUI called.");
            // Default: hide the scenario bar; only show when we actually have scenarios
            scenarioToggleContainer.gameObject.SetActive(false);

            if (ProjectData.Current == null || ProjectData.Current.RootLayer == null)
            {
                Log("ProjectData.Current or RootLayer is NULL. Cannot build scenarios yet.");
                return;
            }
 
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
                    scenarioFolders.Add(layer);
            }
            Log($"Found {scenarioFolders.Count} scenario folder(s) in the project.");

            if (!scenarioFolders.Any())
            {
                Log("No scenario folders found. Hiding scenario container.");
                // container already set inactive above
                return;
            }

            // We have scenarios -> show the bar
            scenarioToggleContainer.gameObject.SetActive(true);

            suppressToggleEvents = true;

            foreach (var folder in scenarioFolders)
            {
                Log($"  Scenario folder: \"{folder.Name}\" (ActiveSelf={folder.ActiveSelf}, ActiveInHierarchy={folder.ActiveInHierarchy})");
                Log($"Creating toggle for scenario folder: \"{folder.Name}\"");

                var toggle = Instantiate(scenarioTogglePrefab, scenarioToggleContainer);
                ScenarioToggleMarker marker = toggle.GetComponent<ScenarioToggleMarker>();
                marker.SetLabel(folder.Name);
                marker.SetScenario(folder);
                toggle.onValueChanged.AddListener(isOn =>
                {
                    Log($"Toggle for scenario \"{folder.Name}\" changed to {(isOn ? "ON" : "OFF")}.");

                    if (suppressToggleEvents)
                    {
                        Log("  (Event suppressed due to internal state change)");
                        return;
                    }
                    if (!isOn)
                    {
                        Log("  Ignoring OFF event (we treat toggles like radio buttons).");
                        return;
                    }

                    ActivateScenario(marker.Scenario);
                });
            }

            Log($"Activating initial scenario: \"{scenarioFolders[0].Name}\"");
            ActivateScenario(scenarioFolders[0]);

            suppressToggleEvents = false;

            Log("RebuildScenarioUI finished.");
        }

        private void ActivateScenario(LayerData activeFolder)
        {
            Log($"SetToggleStates: activeFolder=\"{activeFolder.Name}\"");

            suppressToggleEvents = true;
            foreach (var scenario in scenarios)
            {
                bool shouldBeOn = (scenario.Scenario == activeFolder);
                scenario.Toggle.isOn = shouldBeOn;
                scenario.Scenario.ActiveSelf = shouldBeOn;
                Log($"  Toggle for \"{scenario.Scenario.Name}\" set to {(shouldBeOn ? "ON" : "OFF")}");
            }
            suppressToggleEvents = false;
        }
        
    }
}
