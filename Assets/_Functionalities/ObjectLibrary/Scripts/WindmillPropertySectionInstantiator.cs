using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Functionalities.ObjectLibrary
{
    public class WindmillPropertySectionInstantiator : MonoBehaviour, IPropertySectionInstantiator
    {
        [SerializeField] private WindmillPropertySection propertySectionPrefab;

        public uint SectionIndex => 0;

        public void AddToProperties(RectTransform properties)
        {
            if (!propertySectionPrefab) return;

            var settings = Instantiate(propertySectionPrefab, properties);
            settings.Windmill = GetComponent<Windmill>();
        }
    }
}