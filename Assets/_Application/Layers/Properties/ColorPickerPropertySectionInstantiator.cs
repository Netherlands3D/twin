using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class ColorPickerPropertySectionInstantiator : MonoBehaviour, IPropertySectionInstantiator
    {
        [SerializeField] private ColorPickerPropertySection propertySectionPrefab;

        public void AddToProperties(RectTransform properties)
        {
            if (!propertySectionPrefab) return;

            var settings = Instantiate(propertySectionPrefab, properties);
            settings.LayerGameObject = GetComponent<LayerGameObject>();
        }
    }
}
