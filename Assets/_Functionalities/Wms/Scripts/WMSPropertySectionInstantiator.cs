
using Netherlands3D.ObjectLibrary;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class WMSPropertySectionInstantiator : MonoBehaviour, IPropertySectionInstantiator
    {
        [SerializeField] private WMSPropertySection propertySectionPrefab;

        public void AddToProperties(RectTransform properties)
        {
            if (!propertySectionPrefab) return;

            var settings = Instantiate(propertySectionPrefab, properties);
            settings.Controller = GetComponent<WMSLayerGameObject>();
        }
    }
}