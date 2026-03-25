using System;
using System.Collections.Generic;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Netherlands3D.UI.Panels
{
    public class PropertyPanelBehaviour : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActionAsset;

        private VisualElement root;
        private InputAction rightClickAction;
        private InputAction leftClickAction;
        private InputAction longPressAction;
        private InputAction touchAction;
        private PropertiesPanel propertiesPanel; //main panel for property sections
        private VisualElement propertySectionContainer;
        private List<VisualElement> propertySections = new(); //property sections

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            propertiesPanel = root.Q<PropertiesPanel>("PropertiesPanel");
            propertySectionContainer = propertiesPanel.Q("Content");
            propertiesPanel.Q<Button>().clicked += ClearActivePanel;
        }

        public void ClearActivePanel()
        {
            
            foreach (var section in propertySections)
                propertySectionContainer.Remove(section);
            
            propertySections.Clear();
            propertiesPanel.SetEnabled(false);
        }

        public void SpawnPanel(LayerData layer)
        {
            ClearActivePanel();
            propertiesPanel.SetEnabled(true);
            CheckAndSpawnPanel(layer);
        }

        private void CheckAndSpawnPanel(LayerData layer)
        {
            var hasPanels = false;
            foreach (var property in layer.LayerProperties)
            {
                if (property.IsEditable == false) continue;

                hasPanels |= ShowPanelsForProperty(property, layer.LayerProperties);
            }

            if (!hasPanels)
            {
                ClearActivePanel();
            }
        }

        private bool ShowPanelsForProperty(LayerPropertyData property, List<LayerPropertyData> properties)
        {
            var type = property.GetType();
            if (PropertySectionRegistry.TypeRegistry.ContainsKey(type))
            {
                var panelType = PropertySectionRegistry.TypeRegistry[type];

                var propertySection = (VisualElement)Activator.CreateInstance(panelType);
                propertySections.Add(propertySection);
                propertySectionContainer.Add(propertySection);
                ((IVisualizationWithPropertyData)propertySection).LoadProperties(properties);
                return true;
            }

            return false;
        }
    }
}