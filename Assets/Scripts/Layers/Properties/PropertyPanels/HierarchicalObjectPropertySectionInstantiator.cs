using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class HierarchicalObjectPropertySectionInstantiator : MonoBehaviour, IPropertySectionInstantiator
    {
        [SerializeField] private AbstractHierarchicalObjectPropertySection propertySectionPrefab;

        public AbstractHierarchicalObjectPropertySection PropertySectionPrefab
        {
            get => propertySectionPrefab;
            set => propertySectionPrefab = value;
        }

        public void AddToProperties(RectTransform properties)
        {
            if (!propertySectionPrefab) return;

            var settings = Instantiate(propertySectionPrefab, properties);
            settings.LayerGameObject = GetComponent<HierarchicalObjectLayerGameObject>();
        }
    }
}