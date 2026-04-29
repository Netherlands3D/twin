using System;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.Credentials.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI_Toolkit;
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
        private SecondaryPropertiesPanel secondaryPropertiesPanel;
        private VisualElement propertySectionContainer;
        private ColorPicker colorPicker;

        private void Start()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            propertiesPanel = root.Q<PropertiesPanel>("PropertiesPanel");
            secondaryPropertiesPanel = root.Q<SecondaryPropertiesPanel>();
            colorPicker = secondaryPropertiesPanel.Q<ColorPicker>("PropertiesColorPicker");
            secondaryPropertiesPanel.SetVisible(false);
            propertySectionContainer = propertiesPanel.Q("Content");
            propertiesPanel.Q<Button>().clicked += ClearActivePanel;

            ClearActivePanel();
        }

        public void ClearActivePanel()
        {
            propertySectionContainer.Clear();
            propertiesPanel.SetVisible(false);
        }

        public void SpawnPanel(LayerData layer)
        {
            ClearActivePanel();
            propertiesPanel.SetVisible(true);

            CredentialsRequiredPropertyData credentials = layer.LayerProperties.Get<CredentialsRequiredPropertyData>();
            if (credentials != null && !layer.HasValidCredentials)
            {
                bool showingCredentials = ShowPanelsForProperty(credentials, layer.LayerProperties);
                if (showingCredentials) return;
            }

            CheckAndSpawnPropertyPanels(layer);
        }

        private void CheckAndSpawnPropertyPanels(LayerData layer)
        {
            var hasPanels = false;
            foreach (var property in layer.LayerProperties)
            {
                if (property.IsEditable == false) continue;

                hasPanels |= ShowPanelsForProperty(property, layer.LayerProperties);
                hasPanels |= ShowPanelsForInterfaces(property, layer.LayerProperties);
            }

            if (!hasPanels)
            {
                ClearActivePanel();
            }
        }

        private bool ShowPanelsForInterfaces(LayerPropertyData property, List<LayerPropertyData> properties)
        {
            var interfaces = property.GetType().GetInterfaces();
            var hasPanel = false;
            foreach (var interfaceType in interfaces)
            {
                if (PropertySectionRegistry.TypeRegistry.ContainsKey(interfaceType))
                {
                    var panelTypes = PropertySectionRegistry.TypeRegistry[interfaceType];
                    foreach (var panelType in panelTypes)
                    {
                        CreatePanel(panelType, properties);
                        hasPanel = true;
                    }
                }
            }

            return hasPanel;
        }

        private bool ShowPanelsForProperty(LayerPropertyData property, List<LayerPropertyData> properties)
        {
            var type = property.GetType();
            var hasPanels = PropertySectionRegistry.TypeRegistry.ContainsKey(type);
            if (hasPanels)
            {
                var panelTypes = PropertySectionRegistry.TypeRegistry[type];
                foreach (var panelType in panelTypes)
                {
                    CreatePanel(panelType, properties);
                }
            }

            return hasPanels;
        }

        private void CreatePanel(Type panelType, List<LayerPropertyData> properties)
        {
            var propertySection = (VisualElement)Activator.CreateInstance(panelType);
            propertySectionContainer.Add(propertySection);

            if (propertySection is IPropertyPanelWithColorPicker propertyPanelWithColorPicker)
                propertyPanelWithColorPicker.ColorPicker = colorPicker;
            
            ((IVisualizationWithPropertyData)propertySection).LoadProperties(properties);
        }
    }
}