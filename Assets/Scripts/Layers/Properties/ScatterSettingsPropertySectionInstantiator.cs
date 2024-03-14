using Netherlands3D.Twin.UI.LayerInspector;
using Netherlands3D.Windmills;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class ScatterSettingsPropertySectionInstantiator : MonoBehaviour, IPropertySection
    {
        [SerializeField] private ScatterSettingsPropertySection propertySectionPrefab;

        public void AddToProperties(RectTransform properties)
        {
            if (!propertySectionPrefab) return;

            var propertySection = Instantiate(propertySectionPrefab, properties);
            propertySection.Settings = GetComponent<ObjectScatterLayer>().Settings;
        }
    }
}