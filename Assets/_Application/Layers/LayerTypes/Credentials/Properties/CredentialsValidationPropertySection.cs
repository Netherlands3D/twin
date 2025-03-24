using Netherlands3D.Credentials;
using Netherlands3D.Credentials.StoredAuthorization;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.Credentials.Properties
{
    public class CredentialsValidationPropertySection : MonoBehaviour, ICredentialsPropertySection
    {
        private ICredentialHandlerPanel handlerPanel;

        [SerializeField] private GameObject validCredentialsPanel;
        [SerializeField] private GameObject invalidCredentialsPanel;

        public ICredentialHandlerPanel HandlerPanel
        {
            get => handlerPanel;
            set
            {
                handlerPanel?.OnAuthorizationHandled.RemoveListener(OnCredentialsHandled);

                handlerPanel = value;

                OnCredentialsHandled(handlerPanel.Authorization);
                handlerPanel.OnAuthorizationHandled.AddListener(OnCredentialsHandled);
            }
        }

        private void Start()
        {            
            handlerPanel?.OnAuthorizationHandled.AddListener(OnCredentialsHandled);
        }

        private void OnDestroy()
        {
            handlerPanel?.OnAuthorizationHandled.RemoveListener(OnCredentialsHandled);
        }

        private void OnCredentialsHandled(StoredAuthorization auth)
        {
            var accepted = auth is not FailedOrUnsupported;

            if(accepted)
                gameObject.SetActive(true);
    
            validCredentialsPanel.SetActive(accepted);
            invalidCredentialsPanel.SetActive(!accepted);
        }
    }
}