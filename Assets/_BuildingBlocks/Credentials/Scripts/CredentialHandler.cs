using System;
using Netherlands3D.Credentials.StoredAuthorization;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Credentials
{
    //this is the handler to query the keyvault and return the credentials object to be processed further
    public class CredentialHandler : MonoBehaviour, ICredentialHandler
    {
        [Tooltip("KeyVault Scriptable Object")] 
        [SerializeField] private KeyVault keyVault;

        public Uri Uri { get; set; }

        public string UserName { get; set; }
        public string PasswordOrKeyOrTokenOrCode { get; set; }
        public UnityEvent<StoredAuthorization.StoredAuthorization> OnAuthorizationHandled { get; set; } = new();
        public StoredAuthorization.StoredAuthorization Authorization { get; set; }
        
        //called in the inspector on end edit of url input field
        public void SetUri(string url)
        {
            if (!string.IsNullOrEmpty(url))
                Uri = new Uri(url);
        }

        //called in the inspector on button press
        public void ApplyCredentials()
        {          
            // try to get credentials from keyVault
            print("try get cred: " + Uri);
            keyVault.Authorize(Uri, UserName, PasswordOrKeyOrTokenOrCode);
        }

        public void ClearCredentials()
        {
            UserName = "";
            PasswordOrKeyOrTokenOrCode = "";
        }

        private void Awake()
        {
            keyVault.OnAuthorizationTypeDetermined.AddListener(DeterminedAuthorizationType);
        }

        private void OnDestroy()
        {
            keyVault.OnAuthorizationTypeDetermined.RemoveListener(DeterminedAuthorizationType);
        }

        private void DeterminedAuthorizationType(StoredAuthorization.StoredAuthorization auth)
        {
            if (!auth.InputUri.Equals(Uri)) //ensure the returned authorization is our uri
                return;

            Authorization = auth;         
            OnAuthorizationHandled.Invoke(Authorization);
        }
    }
}