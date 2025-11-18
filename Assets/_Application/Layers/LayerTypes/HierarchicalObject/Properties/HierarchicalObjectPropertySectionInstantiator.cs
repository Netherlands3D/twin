using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties
{
    public class HierarchicalObjectPropertySectionInstantiator : MonoBehaviour//, IPropertySectionInstantiator
    {
        [SerializeField] private PropertySection propertySectionPrefab;

        public PropertySection PropertySectionPrefab
        {
            get => propertySectionPrefab;
            set => propertySectionPrefab = value;
        }

        // public void AddToProperties(RectTransform properties)
        // {
        //     if (!propertySectionPrefab) return;
        //
        //     var settings = Instantiate(propertySectionPrefab, properties);
        //     settings.LayerGameObject = GetComponent<HierarchicalObjectLayerGameObject>();
        // }
    }
}