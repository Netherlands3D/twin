using System.Collections.Generic;
using Netherlands3D.Twin.ExtensionMethods;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.Credentials.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class Properties : MonoBehaviour
    {
        [SerializeField] private GameObject card;
        [SerializeField] private RectTransform sections;
        [SerializeField] private PropertySectionRegistry registry;

        private void Start()
        {
            Hide();
        }
        
        public void Show(LayerData layer)
        {
            card.SetActive(true);
            sections.ClearAllChildren();
            
            CredentialsRequiredPropertyData credentials = layer.LayerProperties.Get<CredentialsRequiredPropertyData>();
            if (credentials != null&& !layer.HasValidCredentials)
            {
                bool showingCredentials = ShowPanelsForProperty(credentials, layer.LayerProperties);
                if (showingCredentials) return;
            }
            
            foreach (var property in layer.LayerProperties)
            {
                if(property.IsEditable == false) continue;
                
                ShowPanelsForProperty(property, layer.LayerProperties);
            }
        }

        private bool ShowPanelsForProperty(LayerPropertyData property, List<LayerPropertyData> properties)
        {
            var type = property.GetType();
            var prefabs = registry.GetPanelPrefabs(type, property);                
            if (prefabs.Count > 0)
            {
                foreach(var prefab in prefabs)
                {
                    var panel = Instantiate(prefab, sections);
                    panel.GetComponent<IVisualizationWithPropertyData>().LoadProperties(properties);
                }
                return true;
            }
            return false;
        }

        public void Hide()
        {
            card.gameObject.SetActive(false);
            sections.ClearAllChildren();
        }

        public bool HasPropertiesWithPanel(LayerData layer)
        {
            foreach (var property in layer.LayerProperties)
            {
                var type = property.GetType();
                if (registry.HasPanel(type))
                    return true;
                
                foreach (var interfaceType in type.GetInterfaces())
                {
                    if (registry.HasPanel(interfaceType))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}