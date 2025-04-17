using Netherlands3D.Twin.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class Properties : MonoBehaviour
    {
        public static Properties Instance { get; private set; }

        [SerializeField] private GameObject card;
        [SerializeField] private RectTransform sections;
        [SerializeField] private GameObject secondary_card;
        [SerializeField] private RectTransform secondary_sections;
        [SerializeField] private GameObject secondary_emptySpace;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                return;
            }

            Destroy(gameObject);
        }

        private void Start()
        {
            Hide();
        }
        
        public void Show(ILayerWithPropertyPanels layer)
        {
            card.SetActive(true);
            sections.ClearAllChildren();
            bool hasSecondary = false;
            foreach (var propertySection in layer.GetPropertySections())
            {
                if (propertySection.SectionIndex == 0)
                    propertySection.AddToProperties(sections);
                else if (propertySection.SectionIndex == 1)
                {
                    if (!hasSecondary)
                    {
                        secondary_card.SetActive(true);
                        GameObject emptySpace = Instantiate(secondary_emptySpace, secondary_sections);
                        emptySpace.SetActive(true);
                    }
                    propertySection.AddToProperties(secondary_sections);
                }
            }
        }

        public void Hide()
        {
            card.gameObject.SetActive(false);
            secondary_card.gameObject.SetActive(false);
            sections.ClearAllChildren();
            secondary_sections.ClearAllChildren();
        }
        
        public static ILayerWithPropertyPanels TryFindProperties(LayerData layer)
        {
            var layerProxy = layer as ReferencedLayerData;

            return (layerProxy == null) ? layer as ILayerWithPropertyPanels : layerProxy.Reference as ILayerWithPropertyPanels;
        }
    }
}