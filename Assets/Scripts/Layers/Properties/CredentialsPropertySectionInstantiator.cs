using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class CredentialsPropertySectionInstantiator : MonoBehaviour, IPropertySectionInstantiator
    {
        [SerializeField] private bool autoApplyCredentials = false;
        [SerializeField] private CredentialsPropertySection propertySectionPrefab;  
        [HideInInspector] public UnityEvent<CredentialsPropertySection> OnCredentialsPropertySectionInstantiated = new();

        public void AddToProperties(RectTransform properties)
        {
            if (!propertySectionPrefab) return;

            var settings = Instantiate(propertySectionPrefab, properties);
            settings.AutoApplyCredentials = autoApplyCredentials;
            settings.LayerWithCredentials = GetComponent<ILayerWithCredentials>();
            OnCredentialsPropertySectionInstantiated.Invoke(settings);
        }
    }
}