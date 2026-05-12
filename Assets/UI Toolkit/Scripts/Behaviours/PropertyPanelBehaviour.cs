using System;
using System.Collections.Generic;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Services;
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
        private ColorPicker colorPicker;

        private void Start()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            propertiesPanel = root.Q<PropertiesPanel>("PropertiesPanel");
            secondaryPropertiesPanel = root.Q<SecondaryPropertiesPanel>();
            colorPicker = secondaryPropertiesPanel.Q<ColorPicker>("PropertiesColorPicker");
            propertiesPanel.Q<Button>().clicked += ClearActivePanel;

            ClearActivePanel();
            
            ObjectSelectorService selectorService = ServiceLocator.GetService<ObjectSelectorService>();
            selectorService.OnSelectLayer.AddListener(SpawnPanel);
            selectorService.OnNoLayerSelected.AddListener(ClearActivePanel); //todo: When the layer panel is converted to UI toolkit, we need to test that this event is not called when clicking the Layer properties button, as this would interfere with opening the properties panel.
        }

        private void OnDestroy()
        {
            ObjectSelectorService selectorService = ServiceLocator.GetService<ObjectSelectorService>();
            selectorService.OnSelectLayer.RemoveListener(SpawnPanel);
            selectorService.OnNoLayerSelected.RemoveListener(ClearActivePanel);
        }

        public void ClearActivePanel()
        {
            propertiesPanel.ClearPropertySections();
            propertiesPanel.SetVisible(false);
            secondaryPropertiesPanel.SetVisible(false);
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

            propertiesPanel.SetButtonsActive();
        }

        private bool ShowPanelsForInterfaces(LayerPropertyData property, List<LayerPropertyData> properties)
        {
            var interfaces = property.GetType().GetInterfaces();
            var hasPanel = false;
            foreach (var interfaceType in interfaces)
            {
                hasPanel |= CreatePanelForType(interfaceType, properties);
            }

            return hasPanel;
        }

        private bool ShowPanelsForProperty(LayerPropertyData property, List<LayerPropertyData> properties)
        {
            var type = property.GetType();
            var hasPanels = CreatePanelForType(type, properties);

            return hasPanels;
        }
        
        private bool CreatePanelForType(Type type, List<LayerPropertyData> properties)
        {
            var hasPanels = false;
            foreach (var categoryCollection in PropertySectionRegistry.TypeRegistry)
            {
                var hasPanelsInCatogory = categoryCollection.Value.Collection.ContainsKey(type);
                if (hasPanelsInCatogory)
                {
                    hasPanels = true;
                    var panelTypes = categoryCollection.Value.Collection[type];
                    foreach (var panelType in panelTypes)
                    {
                        CreatePanel(panelType, categoryCollection.Key, properties);
                    }
                }
            }

            return hasPanels;
        }

        private void CreatePanel(Type panelType, PropertySectionCategory category, List<LayerPropertyData> properties)
        {
            var propertySection = (VisualElement)Activator.CreateInstance(panelType);
            propertiesPanel.AddPropertySection(propertySection, category);

            if (propertySection is IPropertyPanelWithColorPicker propertyPanelWithColorPicker)
                propertyPanelWithColorPicker.ColorPicker = colorPicker;

            ((IVisualizationWithPropertyData)propertySection).LoadProperties(properties);
        }
    }
}