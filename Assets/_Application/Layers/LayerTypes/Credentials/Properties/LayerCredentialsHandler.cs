using Netherlands3D.Credentials;
using Netherlands3D.Twin.ExtensionMethods;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using System;
using Netherlands3D.Credentials.StoredAuthorization;

namespace Netherlands3D.Twin.Layers.LayerTypes.Credentials.Properties
{
    // [RequireComponent(typeof(ILayerWithCredentials))]
    [RequireComponent(typeof(LayerGameObject))]
    public class LayerCredentialsHandler : MonoBehaviour//, ICredentialHandler
    {
        [Tooltip("KeyVault Scriptable Object")] 
        [SerializeField] private KeyVault keyVault;
        [Header("Settings")] 
        [SerializeField] private bool findKeyInVaultOnURLChange = true;
        [SerializeField] private bool autoApplyCredentials = false;

        private Uri baseUri;
        public Uri BaseUri
        {
            get { return baseUri; }
            set
            {
                baseUri = new Uri(value.GetLeftPart(UriPartial.Path));
                // keyVault.Authorize(value, UserName, PasswordOrKeyOrTokenOrCode);
            }
        }
        public string UserName { get; set; }
        public string PasswordOrKeyOrTokenOrCode { get; set; }
        public UnityEvent<StoredAuthorization> OnAuthorizationHandled { get; set; } = new();
        public StoredAuthorization Authorization { get; set; }


        public bool AutoApplyCredentials
        {
            get => autoApplyCredentials;
            set => autoApplyCredentials = value;
        }

        private LayerGameObject layerGameObject;

        public bool HasValidCredentials => layerGameObject && layerGameObject.LayerData.HasValidCredentials;
        public UnityEvent<bool> CredentialsAccepted => layerGameObject.LayerData.HasValidCredentialsChanged;

        public UnityEvent<string> OnURLChanged;
        
      /*  private void Awake()
        {
            // layerWithCredentials = GetComponent<ILayerWithCredentials>();
            layerGameObject = GetComponent<LayerGameObject>();
        }

        private void OnEnable()
        {
            // keyVault.OnAuthorizationTypeDetermined.AddListener(OnCredentialTypeDetermined);
            OnURLChanged.AddListener(UrlHasChanged);
            OnServerResponseReceived.AddListener(HandleServerResponse);

            Url = layerWithCredentials.URL;
        }
        
        public void UpdateUrl(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                BaseUri = new(url);
                UrlHasChanged(new Uri(layerWithCredentials.URL));
            }
        }

        private void OnDisable()
        {
            // keyVault.OnAuthorizationTypeDetermined.RemoveListener(OnCredentialTypeDetermined);

            layerWithCredentials.OnURLChanged.RemoveListener(UrlHasChanged);
            layerWithCredentials.OnServerResponseReceived.RemoveListener(HandleServerResponse);
        }

        public void HandleServerResponse(UnityWebRequest webRequest)
        {
            layerGameObject.LayerData.HasValidCredentials = webRequest.result == UnityWebRequest.Result.Success;

            if (webRequest.RequiresCredentials())
            {
                Debug.LogWarning("Credentials required: " + webRequest);
                //Show a credentials warning if the server request failed for another reason (probably tied to credential failure)
            }
        }

        public void CloseFailedFeedback()
        {
            CredentialsAccepted.Invoke(false);

            //Gob back to our generic guess field type so we can retry again
            // authorizationType = AuthorizationType.InferableSingleKey;
            // SetAuthorizationInputType(authorizationType);
        }

        private void UrlHasChanged(Uri newURL)
        {
            //New url. If we already got this one in the vault, apply the credentials
            if (findKeyInVaultOnURLChange)
            {
                string url = newURL.ToString();

                keyVault.Authorize(url, UserName, PasswordOrKeyOrTokenOrCode, OnCredentialTypeDetermined);

                // if (keyVault.TryGetStoredAuthorization(url, out storedAuthorization))
                // {
                //     Debug.Log("Found stored authorization for: " + newURL + " with type: " + storedAuthorization.AuthorizationType);
                //     authorizationType = storedAuthorization.AuthorizationType;
                //
                //     if (storedAuthorization is Public publicAuthorization)
                //     {                    
                //         CredentialsAccepted.Invoke(true);
                //         return;
                //     }
                //     if (storedAuthorization is InferableSingleKey inferableSingleKey)
                //     {
                //         PasswordOrKeyOrTokenOrCode = inferableSingleKey.key;
                //         CredentialsAccepted.Invoke(true);
                //         return;
                //     }
                //     if (storedAuthorization is UsernamePassword usernamePassword)
                //     {
                //         UserName = usernamePassword.username;
                //         PasswordOrKeyOrTokenOrCode = usernamePassword.password;
                //         CredentialsAccepted.Invoke(true);
                //         return;
                //     }
                // }
                // else
                // {
                //     authorizationType = keyVault.GetKnownAuthorizationTypeForURL(url);
                //     CredentialsAccepted.Invoke(false);
                // }

                // SetAuthorizationInputType(authorizationType);

                if (AutoApplyCredentials)
                    ApplyCredentials();
            }
        }

        // private void OnAuthorizationTypeDetermined(string url, StoredAuthorization auth)
        // {
        //     if (url != layerWithCredentials.URL.ToString()) //todo: check only base uri?
        //         return;
        //     
        //     if(auth is )
        // }

        public void ApplyCredentials()
        {
            keyVault.Authorize(layerWithCredentials.URL, UserName, PasswordOrKeyOrTokenOrCode, OnCredentialTypeDetermined);
            // if (Authorization is UsernamePassword usernamePassword)
            // {
            //     keyVault.TryBasicAuthentication(
            //         layerWithCredentials.URL,
            //         UserName,
            //         PasswordOrKeyOrTokenOrCode
            //     );
            // }
            // else if (Authorization is InferableSingleKey inferableSingleKey)
            // {
            //     keyVault.TryToFindSpecificCredentialType(
            //         layerWithCredentials.URL,
            //         PasswordOrKeyOrTokenOrCode
            //     );
            // }
        }

        private void OnCredentialTypeDetermined(string url, StoredAuthorization type)
        {
            var authorizationType = type.AuthorizationType;

            switch (authorizationType)
            {
                case AuthorizationType.UsernamePassword:
                    layerWithCredentials.SetCredentials(UserName, PasswordOrKeyOrTokenOrCode);
                    break;
                case AuthorizationType.BearerToken:
                    layerWithCredentials.SetBearerToken(PasswordOrKeyOrTokenOrCode);
                    break;
                case AuthorizationType.Key:
                    layerWithCredentials.SetKey(PasswordOrKeyOrTokenOrCode);
                    break;
                case AuthorizationType.Code:
                    layerWithCredentials.SetCode(PasswordOrKeyOrTokenOrCode);
                    break;
                case AuthorizationType.Token:
                    layerWithCredentials.SetToken(PasswordOrKeyOrTokenOrCode);
                    break;
                case AuthorizationType.Public:
                    layerWithCredentials.ClearCredentials();
                    break;
                case AuthorizationType.FailedOrUnsupported:
                    layerWithCredentials.ClearCredentials();
                    CredentialsAccepted.Invoke(false);
                    break;
            }
        }*/

        /// <summary>
        /// Set the authorization input type and update the UI
        /// </summary>
        // public void SetAuthorizationInputType(AuthorizationType type)
        // {
        //     if (
        //         type == AuthorizationType.Key
        //         || type == AuthorizationType.Token
        //         || type == AuthorizationType.BearerToken
        //         || type == AuthorizationType.Code
        //     )
        //         type = AuthorizationType.InferableSingleKey;
        //
        //     authorizationType = type;
        // }
    }
}