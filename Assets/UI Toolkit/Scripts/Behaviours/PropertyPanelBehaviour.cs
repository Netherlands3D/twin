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
            root = GetComponent<UIDocument>().rootVisualElement; //todo ui-toolkit: lets refactor this to use App.UIRoot when implemented fully
            propertiesPanel = root.Q<PropertiesPanel>("PropertiesPanel");
            secondaryPropertiesPanel = root.Q<SecondaryPropertiesPanel>();
            colorPicker = secondaryPropertiesPanel.Q<ColorPicker>("PropertiesColorPicker");
            propertiesPanel.Q<Button>().clicked += ClearActivePanel;

            ClearActivePanel();
            
            ObjectSelectorService selectorService = ServiceLocator.GetService<ObjectSelectorService>();
            selectorService.OnSelectLayer.AddListener(SpawnPanel);
            selectorService.OnNoLayerSelected.AddListener(ClearActivePanel); //todo ui-toolkit: When the layer panel is converted to UI toolkit, we need to test that this event is not called when clicking the Layer properties button, as this would interfere with opening the properties panel.
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
                var credentialPanels = CollectPanelsForType(credentials.GetType());
                SpawnCollectedPanels(credentialPanels, layer.LayerProperties);
                propertiesPanel.UpdateButtonActiveStates();
                if (credentialPanels.Count > 0) return;
            }

            CheckAndSpawnPropertyPanels(layer);
        }

        private void CheckAndSpawnPropertyPanels(LayerData layer)
        {
            var allPanels = new List<(RegisteredPropertySectionType panelType, PropertySectionCategory category)>();

            foreach (var property in layer.LayerProperties)
            {
                if (property.IsEditable == false) continue;

                Type propertyType = property.GetType();
                allPanels.AddRange(CollectPanelsForType(propertyType));

                foreach (var interfaceType in propertyType.GetInterfaces())
                    allPanels.AddRange(CollectPanelsForType(interfaceType));
            }

            allPanels.Sort((a, b) => a.panelType.Order.CompareTo(b.panelType.Order));
            SpawnCollectedPanels(allPanels, layer.LayerProperties);

            if (allPanels.Count == 0)
                ClearActivePanel();

            propertiesPanel.UpdateButtonActiveStates();
        }

        private List<(RegisteredPropertySectionType panelType, PropertySectionCategory category)> CollectPanelsForType(Type type)
        {
            var result = new List<(RegisteredPropertySectionType, PropertySectionCategory)>();
            foreach (var categoryCollection in PropertySectionRegistry.TypeRegistry)
            {
                if (!categoryCollection.Value.Collection.TryGetValue(type, out var panelTypes))
                    continue;

                foreach (var panelType in panelTypes)
                    result.Add((panelType, categoryCollection.Key));
            }
            return result;
        }

        private void SpawnCollectedPanels(List<(RegisteredPropertySectionType panelType, PropertySectionCategory category)> panels, List<LayerPropertyData> properties)
        {
            foreach (var (panelType, category) in panels)
                CreatePanel(panelType.SectionType, category, properties);
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