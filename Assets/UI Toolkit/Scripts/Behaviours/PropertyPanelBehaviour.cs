using System;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Netherlands3D.UI.Panels
{
    [RequireComponent(typeof(UIDocument))]
    public class PropertyPanelBehaviour : MonoBehaviour
    {
        private VisualElement root;
        private PropertiesPanel propertiesPanel; //main panel for property sections
        private VisualElement propertySectionContainer;

        private void Start()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            propertiesPanel = root.Q<PropertiesPanel>("PropertiesPanel");
            propertySectionContainer = propertiesPanel.Q("Content");
            propertiesPanel.Q<Button>().clicked += ClearActivePanel;
        }

        public void ClearActivePanel()
        {
            if(propertySectionContainer.childCount > 0)
                propertySectionContainer.Clear();
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
                propertySectionContainer.Add(propertySection);
                ((IVisualizationWithPropertyData)propertySection).LoadProperties(properties);
                return true;
            }

            return false;
        }
    }
}