using System;
using System.Collections.Generic;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.Credentials.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Netherlands3D.UI.Panels
{
    public class PropertyPanelBehaviour : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        private VisualElement root;
        private PropertiesPanel propertiesPanel; //main panel for property sections
        private SecondaryPropertiesPanel secondaryPropertiesPanel;
        private VisualElement propertySectionContainer;
        private ColorPicker colorPicker;

        public LayerData activeLayer;
        public UnityEvent<LayerData> PropertySectionOpened;
        public UnityEvent<LayerData> PropertySectionClosed;

        private void Start()
        {
            root = uiDocument.rootVisualElement;
            propertiesPanel = root.Q<PropertiesPanel>("PropertiesPanel");
            secondaryPropertiesPanel = root.Q<SecondaryPropertiesPanel>();
            colorPicker = secondaryPropertiesPanel.Q<ColorPicker>("PropertiesColorPicker");
            propertySectionContainer = propertiesPanel.Q("Content");
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
            propertySectionContainer.Clear();
            propertiesPanel.SetVisible(false);
            secondaryPropertiesPanel.SetVisible(false);
            PropertySectionClosed.Invoke(activeLayer);
            activeLayer = null;
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

            activeLayer = layer;
            PropertySectionOpened.Invoke(activeLayer);

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