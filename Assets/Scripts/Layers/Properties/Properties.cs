using Netherlands3D.Twin.Layers.LayerTypes;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class Properties : MonoBehaviour
    {
        public static Properties Instance { get; private set; }

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
        
        public void Show(ILayerWithProperties layer)
        {
            gameObject.SetActive(true);
            sections.ClearAllChildren();
            Debug.Log("Showing properties for " + layer);
            foreach (var propertySection in layer.GetPropertySections())
            {
                propertySection.AddToProperties(sections);
            }
        }

        public void Hide()
        {
            Debug.Log("Hiding properties");
            gameObject.SetActive(false);
            sections.ClearAllChildren();
        }
    }
}