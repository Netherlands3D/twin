using Netherlands3D.Credentials;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.Credentials.Properties
{
    public class CredentialsValidationPropertySection : MonoBehaviour, ICredentialInterface
    {
        private ICredentialHandler handler;

        [SerializeField] private GameObject validCredentialsPanel;
        [SerializeField] private GameObject invalidCredentialsPanel;

        public ICredentialHandler Handler
        {
            get => handler;
            set
            {
                if(handler != null)
                    handler.CredentialsAccepted.RemoveListener(OnCredentialsAccepted);

                handler = value;
                if (!handler.StatusEnabled) return;

                OnCredentialsAccepted(handler.HasValidCredentials);
                handler.CredentialsAccepted.AddListener(OnCredentialsAccepted);
            }
        }

        private void Start()
        {
            if (!handler.StatusEnabled)
            {
                gameObject.SetActive(false);
                return;
            }

            if (handler != null)
                handler.CredentialsAccepted.AddListener(OnCredentialsAccepted);
        }

        private void OnDestroy()
        {
            if (handler != null)
                handler.CredentialsAccepted.RemoveListener(OnCredentialsAccepted);
        }

        private void OnCredentialsAccepted(bool accepted)
        {
            if(accepted)
                gameObject.SetActive(true);
    
            validCredentialsPanel.SetActive(accepted);
            invalidCredentialsPanel.SetActive(!accepted);
        }
    }
}