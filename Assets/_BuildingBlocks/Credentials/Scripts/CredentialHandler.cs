using System;
using Netherlands3D.Services;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Credentials
{
    //this is the handler to query the keyvault and return the credentials object to be processed further
    public class CredentialHandler : MonoBehaviour, ICredentialHandler
    {
        public Uri Uri { get; set; }

        public string UserName { get; set; }
        public string PasswordOrKeyOrTokenOrCode { get; set; }
        public UnityEvent<Uri, StoredAuthorization.StoredAuthorization> OnAuthorizationHandled { get; set; } = new();
        public UnityEvent<Uri, StoredAuthorization.StoredAuthorization> OnAuthorizationUnchanged { get; set; } = new();
        public StoredAuthorization.StoredAuthorization Authorization { get; private set; }

        private KeyVault keyVault;

        private void Awake()
        {
            keyVault = ServiceLocator.GetService<KeyVaultService>().KeyVault;
            keyVault.OnAuthorizationTypeDetermined.AddListener(DeterminedAuthorizationType);
            keyVault.UntestedQueryStringAuthFound.AddListener(OnAuthParsedFromUrl);
        }

        private void OnDestroy()
        {
            keyVault.OnAuthorizationTypeDetermined.RemoveListener(DeterminedAuthorizationType);
            keyVault.UntestedQueryStringAuthFound.RemoveListener(OnAuthParsedFromUrl);
        }

        private void OnAuthParsedFromUrl(string key, string value)
        {
            PasswordOrKeyOrTokenOrCode = value;
        }

        //called in the inspector on button press
        public void ApplyCredentials()
        {
            // try to get credentials from keyVault
            keyVault.Authorize(Uri, UserName, PasswordOrKeyOrTokenOrCode);
        }

        public void ClearCredentials()
        {
            UserName = "";
            PasswordOrKeyOrTokenOrCode = "";
            Authorization = null;
        }

        private void DeterminedAuthorizationType(StoredAuthorization.StoredAuthorization auth)
        {
            if (Uri == null || auth.Domain != new Uri(Uri.GetLeftPart(UriPartial.Path))) //ensure the returned authorization is relevant to us
                return;

            //ensure the new auth is not the same at the one we already have. If it is, we don't need a reload
            if (auth == Authorization) 
            {
                OnAuthorizationUnchanged.Invoke(auth.SanitizeUrl(Uri), auth);
                return;
            }
            Authorization = auth;
            OnAuthorizationHandled.Invoke(auth.SanitizeUrl(Uri), auth);
        }
    }
}