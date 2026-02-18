using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.WSA;

namespace Netherlands3D.Twin.Layers.UI.HierarchyInspector
{
    public class ScenarioManager : MonoBehaviour
    {
        [Header("UI wiring")]
        [SerializeField] private RectTransform scenarioToggleContainer;
        [SerializeField] private Scenario scenarioTogglePrefab;

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
            App.Layers.LayerAdded.AddListener(OnLayerAdded);
            App.Layers.LayerRemoved.AddListener(OnLayerDeleted);
        }

        private void OnDisable()
        {
            App.Layers.LayerAdded.RemoveListener(OnLayerAdded);
            App.Layers.LayerRemoved.RemoveListener(OnLayerDeleted);
        }

        private void OnLayerAdded(LayerData layer)
        {
            layer.NameChanged.AddListener(ConvertToScenario);
            layer.NameChanged.AddListener(ConvertScenarioToFolder);
            layer.NameChanged.AddListener(UpdateLabelForScenario);  

            if (layer.HasProperty<ScenarioPropertyData>())
            {
                RebuildScenarioUI();
            }            
        }
        private void OnLayerDeleted(LayerData layer)
        {
            layer.NameChanged.RemoveListener(ConvertToScenario);
            layer.NameChanged.RemoveListener(ConvertScenarioToFolder);
            layer.NameChanged.RemoveListener(UpdateLabelForScenario);

            if (layer.HasProperty<ScenarioPropertyData>())
            {
                RemoveScenario(layer);
                DeselectScenario(layer);
                if (scenarios.Count == 0)
                    SetScenarioContainerEnabled(false);
                
                RebuildScenarioUI();
            }
        }

        private void ConvertToScenario(LayerData folder, string name)
        {
            if(IsScenarioTag(folder.Name) && folder.HasProperty<FolderPropertyData>())
            {
                folder.SetProperty(new ScenarioPropertyData());
                folder.RemoveProperty(folder.GetProperty<FolderPropertyData>());
                AddScenario(folder);
                SetScenarioContainerEnabled(true);
                SelectScenario(folder);
            }
        }

        private void ConvertScenarioToFolder(LayerData scenario, string name)
        {
            if (!IsScenarioTag(scenario.Name) && scenario.HasProperty<ScenarioPropertyData>())
            {
                scenario.SetProperty(new FolderPropertyData());
                scenario.RemoveProperty(scenario.GetProperty<FolderPropertyData>());
                RemoveScenario(scenario);
                DeselectScenario(scenario);
                if (scenarios.Count == 0)
                    SetScenarioContainerEnabled(false);
            }
        }   
        
        private bool IsScenarioTag(string tag)
        {
            return tag.StartsWith("Scenario:") || tag.StartsWith("scenario:");
        }

        private void AddScenario(LayerData layer)
        {
            Scenario scenario = Instantiate(scenarioTogglePrefab, scenarioToggleContainer);
            scenario.SetLabel(layer.Name);
            scenario.SetLayer(layer);
            scenarios.Add(scenario);
            scenario.VisibilityChanged.AddListener(v => OnScenarioVisiblityChanged(v, scenario));
            
        }

        private void OnScenarioVisiblityChanged(bool visible, Scenario scenario)
        {
            if (visible)
            {
                if (selectedScenario != scenario)
                {
                    if(selectedScenario != null)
                        DeselectScenario(selectedScenario.Layer);
                    SelectScenario(scenario.Layer);
                }
            }
            else
                DeselectScenario(scenario.Layer);
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
                    ActivateScenario(scenario.Layer, true);
                    selectedScenario = scenario;
                    layer.SelectLayer();
                    return;
                }
            }
        }

        private void DeselectScenario(LayerData layer)
        {
            foreach (var scenario in scenarios)
            {
                if (scenario.Layer == layer)
                {
                    ActivateScenario(scenario.Layer, false);
                    if(selectedScenario?.Layer == layer)
                        selectedScenario = null;
                  
                    layer.DeselectLayer();
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
                ConvertToScenario(layer, layer.Name);
                if (layer.HasProperty<ScenarioPropertyData>())
                    AddScenario(layer);
            }
            if (!scenarios.Any())  return;
            
            SetScenarioContainerEnabled(true);
            
            if(selectedScenario?.Layer == scenarios[0].Layer) return;
            
            SelectScenario(scenarios[0].Layer);          
        }

        private void ActivateScenario(LayerData activeFolder, bool activate)
        {
            foreach (var scenario in scenarios)
            {
                if (scenario.Layer == activeFolder)
                {
                    scenario.Toggle.isOn = activate;
                    scenario.Layer.ActiveSelf = activate;
                }
                
            }
        }

        private void UpdateLabelForScenario(LayerData layer, string name)
        {
            if(!layer.HasProperty<ScenarioPropertyData>()) return;

            foreach (Scenario s in scenarios)
            {
                if (s.Layer == layer)
                {
                    s.SetLabel(name);
                    return;
                }
            }
        }
    }
}
