using System;
using System.Collections.Generic;
using Netherlands3D.Twin.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class Properties : MonoBehaviour
    {

        [SerializeField] private GameObject card;
        [SerializeField] private RectTransform sections;
        [SerializeField] private PropertySectionRegistry registry;
        private static PropertySectionRegistry Registry;

        private void Start()
        {
            Hide();
        }
        
        public void Show(LayerData layer)
        {
            card.SetActive(true);
            sections.ClearAllChildren();
            
            foreach (var property in layer.LayerProperties)
            {
                PropertySection prefab = registry.GetPrefab(property.GetType());
                if (prefab)
                {
                    var panel = Instantiate(prefab, sections);
                    panel.LoadProperties(layer.LayerProperties);
                }
            }
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
                if (registry.HasPanel(property.GetType()))
                    return true;
            }

            return false;
        }
        
        // public static ILayerWithPropertyPanels TryFindProperties(LayerData layer)
        // {
        //     LayerGameObject template = ProjectData.Current.PrefabLibrary.GetPrefabById(layer.PrefabIdentifier); //todo: this now gives an error if the prefabID is not in the library (e.g. folderLayers)
        //     return (template == null) ? layer as ILayerWithPropertyPanels : template as ILayerWithPropertyPanels;
        // }
    }
}