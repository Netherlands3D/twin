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
            var handler = GetComponent<ICredentialHandler>();
            
            //maybe we should make this class more explicit for layers
            foreach (var credentialInterface in settings.GetComponentsInChildren<ICredentialInterface>(true))
            {
                credentialInterface.Handler = handler;
            }
        }
    }
}