using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class CredentialsPropertySectionInstantiator : MonoBehaviour, IPropertySectionInstantiator
    {
        [SerializeField] private CredentialsPropertySection propertySectionPrefab;
        
        public void AddToProperties(RectTransform properties)
        {
            if (!propertySectionPrefab) return;

            var settings = Instantiate(propertySectionPrefab, properties);
            settings.LayerWithCredentials = GetComponent<ILayerWithCredentials>();
        }
    }
}