using Netherlands3D.Twin.Layers.LayerTypes.Credentials.Properties;
using System;
using Netherlands3D.Credentials.StoredAuthorization;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Credentials
{
    //this is the handler for the ui flow to check credentials
    //will be active in the layerpanel when trying to add a layer through url
    //we also use this credentialhandler for storing credential data while not having active layers yet
    public class CredentialHandler : MonoBehaviour, ICredentialHandler
    {
        [Tooltip("KeyVault Scriptable Object")] 
        [SerializeField] private KeyVault keyVault;
        
        private Uri baseUri;
        private Uri inputUri;

        public Uri BaseUri
        {
            get { return baseUri; }
            set
            {
                inputUri = value;
                baseUri = new Uri(value.GetLeftPart(UriPartial.Path));
            }
        }
        public string UserName { get; set; }
        public string PasswordOrKeyOrTokenOrCode { get; set; }
        public UnityEvent<StoredAuthorization.StoredAuthorization> OnAuthorizationHandled { get; set; } = new();
        public StoredAuthorization.StoredAuthorization Authorization { get; set; }
        
        //needed to start for example the DataTypeChain
        public UnityEvent<string> CredentialsSucceeded { get { return OnCredentialsSucceeded; } set { OnCredentialsSucceeded = value; } }
        public UnityEvent<string> OnCredentialsSucceeded = new();
        
        //called in the inspector on end edit of url input field
        public void SetUri(string url)
        {
            if (!string.IsNullOrEmpty(url))
                BaseUri = new Uri(url);
        }

        //called in the inspector on button press
        public void ApplyCredentials()
        {          
            // try to get credentials from keyVault
            keyVault.Authorize(inputUri, UserName, PasswordOrKeyOrTokenOrCode);
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
            if (!auth.baseUri.Equals(BaseUri)) //ensure the returned authorization is our uri
                return;

            Authorization = auth;            

            //we check if the authorized type is different from unknown. The keyvault webrequests can never return unknown if authorization was valid          
            if(Authorization is not FailedOrUnsupported)
                CredentialsSucceeded.Invoke(auth.GetUriWithCredentials().ToString());
            
            OnAuthorizationHandled.Invoke(Authorization);
        }
    }
}