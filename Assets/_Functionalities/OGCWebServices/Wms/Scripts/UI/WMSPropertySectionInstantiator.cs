using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Functionalities.OgcWebServices.Wms.UI
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