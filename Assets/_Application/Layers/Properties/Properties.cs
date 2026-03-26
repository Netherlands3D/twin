using System.Collections.Generic;
using Netherlands3D.Services;
using Netherlands3D.Twin.ExtensionMethods;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.Credentials.Properties;
using Netherlands3D.UI.Panels;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class Properties : MonoBehaviour
    {
        [SerializeField] private GameObject card;
        [SerializeField] private RectTransform sections;
        // [SerializeField] private PropertySectionRegistry registry;

        private void Start()
        {
            Hide();
        }

        public void Show(LayerData layer)
        {
            //UI Toolkit section todo: remove the rest of this function, and possibly this entire script once the LayerUI is converted to UI toolkit
            var propertyPanelBehaviour = FindAnyObjectByType<PropertyPanelBehaviour>();
            propertyPanelBehaviour.SpawnPanel(layer);
            //---

            card.SetActive(true);
            sections.ClearAllChildren();

            CredentialsRequiredPropertyData credentials = layer.LayerProperties.Get<CredentialsRequiredPropertyData>();
            if (credentials != null && !layer.HasValidCredentials)
            {
                bool showingCredentials = ShowPanelsForProperty(credentials, layer.LayerProperties);
                if (showingCredentials) return;
            }

            foreach (var property in layer.LayerProperties)
            {
                if (property.IsEditable == false) continue;

                ShowPanelsForProperty(property, layer.LayerProperties);
            }
        }

        private bool ShowPanelsForProperty(LayerPropertyData property, List<LayerPropertyData> properties)
        {
            var type = property.GetType();
            var panelExists = PropertySectionRegistry.TypeRegistry.ContainsKey(type);
            if(!panelExists)
                Debug.LogError("Missing PropertySection for: " + type + " UI toolkit work in progress, if these errors show up the transition of property panels to UI toolkit is not complete yet");
            return panelExists;

            // var prefabs = registry.GetPanelPrefabs(type, property);                
            // if (prefabs.Count > 0)
            // {
            //     foreach(var prefab in prefabs)
            //     {
            //         var panel = Instantiate(prefab, sections);
            //         panel.GetComponent<IVisualizationWithPropertyData>().LoadProperties(properties);
            //     }
            //     return true;
            // }
            // return false;
        }

        public void Hide()
        {
            var propertyPanelBehaviour = FindAnyObjectByType<PropertyPanelBehaviour>();
            propertyPanelBehaviour.ClearActivePanel();

            //todo: this is no longer needed after the transition to UI toolkit
            card.gameObject.SetActive(false);
            sections.ClearAllChildren();
        }

        public bool HasPropertiesWithPanel(LayerData layer)
        {
            foreach (var property in layer.LayerProperties)
            {
                var type = property.GetType();
                // if (registry.HasPanel(type))
                if (PropertySectionRegistry.TypeRegistry.ContainsKey(type))
                    return true;

                foreach (var interfaceType in type.GetInterfaces())
                {
                    // if (registry.HasPanel(interfaceType))
                    if (PropertySectionRegistry.TypeRegistry.ContainsKey(type))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}