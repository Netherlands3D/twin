using Netherlands3D.Twin.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Projects;
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
            LayerGameObject template = ProjectData.Current.PrefabLibrary.GetPrefabById(layer.PrefabIdentifier); //todo: this now gives an error if the prefabID is not in the library (e.g. folderLayers)
            return (template == null) ? layer as ILayerWithPropertyPanels : template as ILayerWithPropertyPanels;
        }
    }
}