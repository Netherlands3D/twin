using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.Credentials.Properties
{
    public class CredentialsValidationPropertySection : MonoBehaviour, ILayerCredentialInterface
    {
        private LayerCredentialsHandler handler;

        [SerializeField] private GameObject validCredentialsPanel;
        [SerializeField] private GameObject invalidCredentialsPanel;

        public LayerCredentialsHandler Handler
        {
            get => handler;
            set
            {
                if (handler)
                    handler.CredentialsAccepted.RemoveListener(OnCredentialsAccepted);

                handler = value;

                OnCredentialsAccepted(handler.HasValidCredentials);

                handler.CredentialsAccepted.AddListener(OnCredentialsAccepted);
            }
        }

        private void Start()
        {
            if (handler)
                handler.CredentialsAccepted.AddListener(OnCredentialsAccepted);
        }

        private void OnDestroy()
        {
            if (handler)
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