using System;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.UI.LayerInspector;
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

        private void OnEnable()
        {
            ProjectData.Current.LayerAdded.AddListener(OnLayerAdded);
        }

        private void OnDisable()
        {
            ProjectData.Current.LayerAdded.RemoveListener(OnLayerAdded);
        }

        private void Start()
        {
            Hide();
        }
        
        private void OnLayerAdded(LayerNL3DBase layer)
        {
            var propertiesLayer = TryFindProperties(layer);
            if (propertiesLayer != null)
            {
                Show(propertiesLayer);
            }
        }
        
        public void Show(ILayerWithProperties layer)
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
        
        public static ILayerWithProperties TryFindProperties(LayerNL3DBase layer)
        {
            var layerProxy = layer as ReferencedProxyLayer;

            return (layerProxy == null) ? layer as ILayerWithProperties : layerProxy.Reference as ILayerWithProperties;
        }
    }
}