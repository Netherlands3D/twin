using Netherlands3D.Functionalities.ObjectLibrary;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class PropertySectionInstantiator : MonoBehaviour, IPropertySectionInstantiator
    {
        [SerializeField] private PropertySectionWithLayerGameObject propertySectionPrefab;

        public void AddToProperties(RectTransform properties)
        {
            if (propertySectionPrefab == null) return;

            var settings = Instantiate(propertySectionPrefab, properties);
            settings.LayerGameObject = GetComponent<LayerGameObject>();
        }
    }
}