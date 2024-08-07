using Netherlands3D.Twin.Layers.LayerTypes;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class Properties : MonoBehaviour
    {
        public static Properties Instance { get; private set; }

        [SerializeField] private GameObject card;
        [SerializeField] private RectTransform sections;
        
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
            foreach (var propertySection in layer.GetPropertySections())
            {
                propertySection.AddToProperties(sections);
            }
        }

        public void Hide()
        {
            card.gameObject.SetActive(false);
            sections.ClearAllChildren();
        }
        
        public static ILayerWithPropertyPanels TryFindProperties(LayerData layer)
        {
            var layerProxy = layer as ReferencedLayerData;

            return (layerProxy == null) ? layer as ILayerWithPropertyPanels : layerProxy.Reference as ILayerWithPropertyPanels;
        }
    }
}