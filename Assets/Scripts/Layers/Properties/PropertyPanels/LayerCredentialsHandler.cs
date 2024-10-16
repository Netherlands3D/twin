using System;
using Netherlands3D.Twin.Layers.LayerTypes;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [RequireComponent(typeof(ILayerWithCredentials))]
    [RequireComponent(typeof(LayerGameObject))]
    public class LayerCredentialsHandler : MonoBehaviour
    {
        public string UserName { get; set; }
        public string PasswordOrKeyOrTokenOrCode { get; set; }

        [Header("Settings")] 
        [SerializeField] private bool findKeyInVaultOnURLChange = true;

        [Tooltip("KeyVault Scriptable Object")] 
        [SerializeField] private KeyVault keyVault;

        private AuthorizationType authorizationType = AuthorizationType.Public;
        private StoredAuthorization storedAuthorization;

        [SerializeField] private bool autoApplyCredentials = false;

        public bool AutoApplyCredentials
        {
            get => autoApplyCredentials;
            set => autoApplyCredentials = value;
        }

        public AuthorizationType AuthorizationType => authorizationType;

        private ILayerWithCredentials layerWithCredentials;
        private LayerGameObject layerGameObject;

        public bool HasValidCredentials => layerGameObject && layerGameObject.LayerData.HasValidCredentials;
        public UnityEvent<bool> CredentialsAccepted => layerGameObject.LayerData.HasValidCredentialsChanged;

        private void Awake()
        {
            layerWithCredentials = GetComponent<ILayerWithCredentials>();
            layerGameObject = GetComponent<LayerGameObject>();
        }

        private void OnEnable()
        {
            keyVault.OnAuthorizationTypeDetermined.AddListener(OnCredentialTypeDetermined);
            layerWithCredentials.OnURLChanged.AddListener(UrlHasChanged);
            layerWithCredentials.OnServerResponseReceived.AddListener(HandleServerResponse);

            UrlHasChanged(layerWithCredentials.URL);
        }

        private void OnDisable()
        {
            keyVault.OnAuthorizationTypeDetermined.RemoveListener(OnCredentialTypeDetermined);

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
            authorizationType = AuthorizationType.InferableSingleKey;
            SetAuthorizationInputType(authorizationType);
        }

        private void UrlHasChanged(string newURL)
        {
            //New url. If we already got this one in the vault, apply the credentials
            if (findKeyInVaultOnURLChange)
            {
                storedAuthorization = keyVault.GetStoredAuthorization(newURL);

                if (storedAuthorization != null)
                {
                    Debug.Log("Found stored authorization for: " + newURL + " with type: " + storedAuthorization.authorizationType);
                    authorizationType = storedAuthorization.authorizationType;
                    CredentialsAccepted.Invoke(true);
                }
                else
                {
                    authorizationType = keyVault.GetKnownAuthorizationTypeForURL(newURL);
                    CredentialsAccepted.Invoke(false);
                }

                SetAuthorizationInputType(authorizationType);

                if (AutoApplyCredentials)
                    ApplyCredentials();
            }
        }

        public void ApplyCredentials()
        {
            switch (authorizationType)
            {
                case AuthorizationType.UsernamePassword:
                    keyVault.TryBasicAuthentication(
                        layerWithCredentials.URL,
                        UserName,
                        PasswordOrKeyOrTokenOrCode
                    );
                    break;
                case AuthorizationType.InferableSingleKey:
                    keyVault.TryToFindSpecificCredentialType(
                        layerWithCredentials.URL,
                        PasswordOrKeyOrTokenOrCode
                    );
                    break;
            }
        }

        private void OnCredentialTypeDetermined(string url, AuthorizationType type)
        {
            authorizationType = type;

            switch (type)
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
                case AuthorizationType.Unknown:
                    layerWithCredentials.ClearCredentials();
                    CredentialsAccepted.Invoke(false);
                    break;
            }
        }

        /// <summary>
        /// Set the authorization input type and update the UI
        /// </summary>
        public void SetAuthorizationInputType(AuthorizationType type)
        {
            if (
                type == AuthorizationType.Key
                || type == AuthorizationType.Token
                || type == AuthorizationType.BearerToken
                || type == AuthorizationType.Code
            )
                type = AuthorizationType.InferableSingleKey;

            authorizationType = type;
        }
    }
}