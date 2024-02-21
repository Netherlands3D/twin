using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class HierarchicalObjectPropertySectionInstantiator : MonoBehaviour, IPropertySectionInstantiator
    {
        [SerializeField] private AbstractHierarchicalObjectPropertySection propertySectionPrefab;

        public void AddToProperties(RectTransform properties)
        {
            if (!propertySectionPrefab) return;

            var settings = Instantiate(propertySectionPrefab, properties);
            settings.Layer = GetComponent<HierarchicalObjectLayer>();
        }
    }
}