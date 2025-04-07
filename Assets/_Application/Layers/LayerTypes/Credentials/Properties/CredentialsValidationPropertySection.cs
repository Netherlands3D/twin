using System;
using Netherlands3D.Credentials;
using Netherlands3D.Credentials.StoredAuthorization;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.Credentials.Properties
{
    public class CredentialsValidationPropertySection : MonoBehaviour, ICredentialsPropertySection
    {
        private ICredentialHandler handler;

        [SerializeField] private GameObject validCredentialsPanel;
        [SerializeField] private GameObject invalidCredentialsPanel;

        public ICredentialHandler Handler
        {
            get => handler;
            set
            {
                handler?.OnAuthorizationHandled.RemoveListener(OnCredentialsHandled);

                handler = value;

                OnCredentialsHandled(handler.Uri, handler.Authorization);
                handler.OnAuthorizationHandled.AddListener(OnCredentialsHandled);
            }
        }

        private void Start()
        {            
            handler?.OnAuthorizationHandled.AddListener(OnCredentialsHandled);
        }

        private void OnDestroy()
        {
            handler?.OnAuthorizationHandled.RemoveListener(OnCredentialsHandled);
        }

        private void OnCredentialsHandled(Uri uri, StoredAuthorization auth)
        {
            var accepted = auth is not FailedOrUnsupported;

            if(accepted)
                gameObject.SetActive(true);
    
            validCredentialsPanel.SetActive(accepted);
            invalidCredentialsPanel.SetActive(!accepted);
        }
    }
}