using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class LayerColorPropertySectionInstantiator : MonoBehaviour, IPropertySectionInstantiator
    {
        [SerializeField] private LayerColorPropertySection propertySectionPrefab;

        public uint SectionIndex => 0;

        public void AddToProperties(RectTransform properties)
        {
            if (!propertySectionPrefab) return;

            var settings = Instantiate(propertySectionPrefab, properties);
            settings.LayerGameObject = GetComponent<LayerGameObject>();
        }
    }
}
