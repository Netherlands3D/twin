using Netherlands3D.Credentials;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.Credentials.Properties
{
    public class CredentialsPropertySectionInstantiator : MonoBehaviour, IPropertySectionInstantiator
    {
        [SerializeField] private GameObject inputPropertySectionPrefab;

        public void AddToProperties(RectTransform properties)
        {
            if (!inputPropertySectionPrefab) return;

            var settings = Instantiate(inputPropertySectionPrefab, properties);
            var handler = GetComponent<ICredentialHandlerPanel>();
            foreach (var credentialInterface in settings.GetComponentsInChildren<ICredentialsPropertySection>(true))
            {
                credentialInterface.HandlerPanel = handler;
            }
        }
    }
}