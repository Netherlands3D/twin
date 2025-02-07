using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class GoToUrlPropertySectionInstantiator : MonoBehaviour, IPropertySectionInstantiator
    {
        [SerializeField] private GoToUrlPropertySection propertySectionPrefab;
        public GoToUrlPropertySection PropertySection { get; private set; }

        public void AddToProperties(RectTransform properties)
        {
            PropertySection = Instantiate(propertySectionPrefab, properties);
            PropertySection.Controller = GetComponent<LayerWithUrl>();
        }
    }
}