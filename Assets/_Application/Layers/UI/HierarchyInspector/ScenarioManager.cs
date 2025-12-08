using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Projects;
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

        private class ScenarioEntry
        {
            public FolderLayer Folder;
            public Toggle Toggle;
        }

        private readonly List<ScenarioEntry> scenarios = new();
        private bool suppressToggleEvents;

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
            ProjectData.Current.LayerAdded.AddListener(OnLayerAdded);
            ProjectData.Current.LayerDeleted.AddListener(OnLayerDeleted);
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
            ProjectData.Current.LayerAdded.RemoveListener(OnLayerAdded);
            ProjectData.Current.LayerDeleted.RemoveListener(OnLayerDeleted);

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

        /// <summary>
        /// Convention: any FolderLayer with name starting with "Scenario:" is a Scenario.
        /// </summary>
        private bool IsScenarioFolder(FolderLayer folder)
        {
            bool isScenario = folder.Name != null
                              && folder.Name.StartsWith("Scenario:", StringComparison.OrdinalIgnoreCase);

            if (debugLogging)
            {
                Log($"IsScenarioFolder? name=\"{folder.Name}\" -> {isScenario}");
            }

            return isScenario;
        }

        private void RebuildScenarioUI()
        {
            Log("RebuildScenarioUI called.");

            if (!scenarioToggleContainer || !scenarioTogglePrefab)
            {
                Log("ScenarioToggleContainer or ScenarioTogglePrefab is NOT assigned in the inspector.");
                return;
            }

            // Default: hide the scenario bar; only show when we actually have scenarios
            scenarioToggleContainer.gameObject.SetActive(false);

            if (ProjectData.Current == null || ProjectData.Current.RootLayer == null)
            {
                Log("ProjectData.Current or RootLayer is NULL. Cannot build scenarios yet.");
                return;
            }

            // Clear existing UI
            foreach (Transform child in scenarioToggleContainer)
            {
                if (child.GetComponent<ScenarioToggleMarker>())
                    Destroy(child.gameObject);
            }
            scenarios.Clear();

            // Find scenario folders in the whole layer tree
            var scenarioFolders = FindScenarioFolders().ToList();
            Log($"Found {scenarioFolders.Count} scenario folder(s) in the project.");

            foreach (var f in scenarioFolders)
            {
                Log($"  Scenario folder: \"{f.Name}\" (ActiveSelf={f.ActiveSelf}, ActiveInHierarchy={f.ActiveInHierarchy})");
            }

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
                Log($"Creating toggle for scenario folder: \"{folder.Name}\"");

                var toggle = Instantiate(scenarioTogglePrefab, scenarioToggleContainer);
                toggle.isOn = false;

                // Use the exact folder name as label (minus the 'Scenario:' prefix if you want)
                string label = folder.Name;
                int idx = label.IndexOf(':');
                if (idx >= 0 && idx + 1 < label.Length)
                    label = label[(idx + 1)..].Trim(); // keep this if you want shorter labels
                // If you want the full name including "Scenario: ", just comment the 2 lines above
                // and use: string label = folder.Name;

                // TMP label first
                TMP_Text tmpText = toggle.GetComponentInChildren<TMP_Text>();
                if (tmpText)
                {
                    tmpText.text = label;
                    Log($"  TMP_Text label set to \"{label}\"");
                }
                else
                {
                    // Fallback to legacy Text if needed
                    var text = toggle.GetComponentInChildren<Text>();
                    if (text)
                    {
                        text.text = label;
                        Log($"  UnityEngine.UI.Text label set to \"{label}\"");
                    }
                    else
                    {
                        Log("  WARNING: No TMP_Text or Text component found under toggle prefab.");
                    }
                }
                // Auto resize the toggle based on text width
                var autoSize = toggle.GetComponent<AutoSizeTMPWidth>();
                if (autoSize != null)
                {
                    autoSize.ResizeNow();
                }

                var entry = new ScenarioEntry { Folder = folder, Toggle = toggle };
                scenarios.Add(entry);

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

                    ActivateScenario(entry.Folder);
                    SetToggleStates(entry.Folder);
                });
            }

            Log($"Activating initial scenario: \"{scenarioFolders[0].Name}\"");
            ActivateScenario(scenarios[0].Folder);
            SetToggleStates(scenarios[0].Folder);

            suppressToggleEvents = false;

            Log("RebuildScenarioUI finished.");
        }

        private IEnumerable<FolderLayer> FindScenarioFolders()
        {
            var root = ProjectData.Current.RootLayer;
            Log($"FindScenarioFolders starting from RootLayer. Root has {root.ChildrenLayers.Count} direct children.");

            return FindScenarioFoldersRecursive(root);
        }

        private IEnumerable<FolderLayer> FindScenarioFoldersRecursive(LayerData layer)
        {
            foreach (var child in layer.ChildrenLayers)
            {
                if (child is FolderLayer folder && IsScenarioFolder(folder))
                    yield return folder;

                foreach (var sub in FindScenarioFoldersRecursive(child))
                    yield return sub;
            }
        }

        private void SetToggleStates(FolderLayer activeFolder)
        {
            Log($"SetToggleStates: activeFolder=\"{activeFolder.Name}\"");

            suppressToggleEvents = true;
            foreach (var scenario in scenarios)
            {
                bool shouldBeOn = (scenario.Folder == activeFolder);
                scenario.Toggle.isOn = shouldBeOn;
                Log($"  Toggle for \"{scenario.Folder.Name}\" set to {(shouldBeOn ? "ON" : "OFF")}");
            }
            suppressToggleEvents = false;
        }

        private void ActivateScenario(FolderLayer activeFolder)
        {
            Log($"ActivateScenario called for folder: \"{activeFolder.Name}\"");

            var allScenarioFolders = scenarios.Select(s => s.Folder).ToList();

            foreach (var folder in allScenarioFolders)
            {
                bool shouldBeActive = (folder == activeFolder);
                Log($"  Setting ActiveSelf for \"{folder.Name}\" -> {shouldBeActive}");
                folder.ActiveSelf = shouldBeActive;
            }
        }
    }
}
