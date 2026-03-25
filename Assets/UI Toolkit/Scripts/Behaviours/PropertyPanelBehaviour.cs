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
        [SerializeField] private PropertySectionRegistry registry;

        private VisualElement root;
        private InputAction rightClickAction;
        private InputAction leftClickAction;
        private InputAction longPressAction;
        private InputAction touchAction;
        private PropertiesPanel propertiesPanel; //main panel for property sections
        private VisualElement propertySectionContainer;
        private List<ContentContainer> propertySections = new(); //property sections

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            propertiesPanel = root.Q<PropertiesPanel>("PropertiesPanel");
            propertySectionContainer = propertiesPanel.Q("Content");
            propertiesPanel.Q<Button>().clicked += ClearActivePanel;
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
            ClearActivePanel();
            propertiesPanel.SetEnabled(true);
            CheckAndSpawnPanel(layer);
        }

        private void CheckAndSpawnPanel(LayerData layer)
        {
            foreach (var property in layer.LayerProperties)
            {
                if(property.IsEditable == false) continue;
                
                ShowPanelsForProperty(property, layer.LayerProperties);
            }
        }

        private bool ShowPanelsForProperty(LayerPropertyData property, List<LayerPropertyData> properties)
        {
            var type = property.GetType();
            //todo temp
            if (type == typeof(TransformLayerPropertyData))
            {
                var propertySection = new TransformPanel();
                propertySectionContainer.Add(propertySection);
                propertySection.LoadProperties(properties);
                
                return true;
            }
            
            // if (layer.LayerProperties.Count > 0)
            // {
            //     Debug.Log("spawn panels for: " + layer.Name);
            //     propertySectionContainer.Add(new PropertySection()); //todo
            //     return true;
            // }

            return false;
        }
    }
}