using Netherlands3D.ObjectLibrary;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class SensorPropertySectionInstantiator : MonoBehaviour, IPropertySectionInstantiator
    {
        [SerializeField] private SensorPropertySection propertySectionPrefab;

        public void AddToProperties(RectTransform properties)
        {
            if (!propertySectionPrefab) return;

            var settings = Instantiate(propertySectionPrefab, properties);
            settings.Controller = GetComponent<SensorDataController>();
        }
    }
}