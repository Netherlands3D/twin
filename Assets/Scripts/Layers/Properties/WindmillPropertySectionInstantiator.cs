using Netherlands3D.Windmills;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class WindmillPropertySectionInstantiator : MonoBehaviour, IPropertySection
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