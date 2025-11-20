using Netherlands3D.Twin.ExtensionMethods;
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
                var type = property.GetType();
                var prefab = registry.GetPrefab(type);
                if (prefab == null)
                {
                    foreach (var interfaceType in type.GetInterfaces())
                    {
                        if (!registry.HasPanel(interfaceType)) continue;
                        
                        prefab = registry.GetPrefab(interfaceType);
                        break;
                    }
                }
                
                if (prefab!=null)
                {
                    var panel = Instantiate(prefab, sections);
                    panel.GetComponent<IVisualizationWithPropertyData>().LoadProperties(layer.LayerProperties);
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