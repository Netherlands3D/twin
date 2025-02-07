using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class ImagePropertySectionInstantiator : MonoBehaviour, IPropertySectionInstantiator
    {
        [SerializeField] private ImagePropertySection propertySectionPrefab;
        public ImagePropertySection PropertySection { get; private set; }

        public void AddToProperties(RectTransform properties)
        {
            PropertySection = Instantiate(propertySectionPrefab, properties);
            PropertySection.Controller = GetComponent<LayerWithImage>();
        }
    }
}