using Netherlands3D.ObjectLibrary;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class WindmillPropertySectionInstantiator : MonoBehaviour, IPropertySectionInstantiator
    {
        [SerializeField] private WindmillPropertySection propertySectionPrefab;

        public void AddToProperties(RectTransform properties)
        {
            if (!propertySectionPrefab) return;

            var settings = Instantiate(propertySectionPrefab, properties);
            settings.Windmill = GetComponent<Windmill>();
        }
    }
}