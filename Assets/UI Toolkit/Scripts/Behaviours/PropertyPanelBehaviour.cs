using System.Collections.Generic;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    public class PropertyPanelBehaviour : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActionAsset;
        [SerializeField] private PropertySectionRegistry registry;

        private VisualElement root;
        private InputAction rightClickAction;
        private InputAction leftClickAction;
        private InputAction longPressAction;
        private InputAction touchAction;
        private PropertiesPanel propertiesPanel; //main conatiner for property sections
        private List<ContentContainer> propertySections = new(); //property sections

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            propertiesPanel = root.Q<PropertiesPanel>("PropertiesPanel");
        }

        void OnDisable()
        {
            // ClearActivePanel();
        }
        
        public void ClearActivePanel()
        {
            propertiesPanel.SetEnabled(false);

            if (propertySections.Count == 0) //todo
                return;

            foreach (var section in propertySections)
                propertiesPanel.Remove(section);
        }

        public void SpawnPanel(LayerData layer)
        {
            propertiesPanel.SetEnabled(true);
            CheckAndSpawnPanel(layer);
        }

        private void CheckAndSpawnPanel(LayerData layer)
        {
            if (!ShowPanelsForProperty(layer))
            {
                Debug.Log(layer.Name + " has no property sections");
            }
        }

        private bool ShowPanelsForProperty(LayerData layer)
        {
            // var type = property.GetType();
            // var prefabs = registry.GetPanelPrefabs(type, property);
            if (layer.LayerProperties.Count > 0)
            {

                    // var panel = Instantiate(prefab, sections);
                    Debug.Log("spawn panels for: " + layer.Name);
                    // panel.GetComponent<IVisualizationWithPropertyData>().LoadProperties(properties);

                return true;
            }

            return false;
        }
    }
}