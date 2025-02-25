using Netherlands3D.Twin.ExtensionMethods;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.Credentials;
using Netherlands3D.Twin.Layers.LayerTypes.Credentials.Properties;
using Netherlands3D.Twin.Layers.UI.HierarchyInspector;
using Netherlands3D.Twin.Tools.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Netherlands3D.Credentials
{
    //this is the handler for the ui flow to check credentials
    //will be active in the layerpanel when trying to add a layer through url
    //we also use this credentialhandler for storing credential data while not having active layers yet
    public class CredentialHandler : MonoBehaviour, ICredentialHandler, ICredentialsObject
    {
        public bool StatusEnabled => false; //no status here because the layer panel will close on authentication
        public string UserName { get; set; }
        public string PasswordOrKeyOrTokenOrCode { get; set; }
        public AuthorizationType AuthorizationType => authorizationType;
                
        public UnityEvent<bool> CredentialsAccepted => null;
        public UnityEvent<string> CredentialsSucceeded = new();
        public bool HasValidCredentials => hasValidCredentials;

        public string URL { get => url; set => url = value; }

        private bool hasValidCredentials = false;

        private AuthorizationType authorizationType = AuthorizationType.Unknown;

        private Dictionary<string, string> customHeaders = new Dictionary<string, string>();
        public Dictionary<string, string> CustomHeaders { get => customHeaders; private set => customHeaders = value; }

        [Header("API Key (Optional)")]
        [Tooltip("Public API key for production use. This key will be used in production builds.")]
        public string publicKey;
        [Tooltip("Personal API key for testing purposes. This key will override the public key in Unity editor.")]
        public string personalKey;
        [Tooltip("The key name to use for the API key in the query string like 'key', or 'code' etc. Default is 'key' for Google Maps API.")]
        [SerializeField] private string queryKeyName = "key";
        public string QueryKeyName { get => queryKeyName; set => queryKeyName = value; }
        public string CredentialQuery { get; private set; } = string.Empty;
        public string tilesetUrl = "https://storage.googleapis.com/ahp-research/maquette/kadaster/3dbasisvoorziening/test/landuse_1_1/tileset.json";

        [Tooltip("KeyVault Scriptable Object")]
        [SerializeField] private KeyVault keyVault;
        private string url;

        private void Awake()
        {
            //were are not using an instantiator for the user interface, so lets set it here
            ICredentialInterface view = GetComponentInChildren<ICredentialInterface>();
            view.Handler = this;

            keyVault.OnAuthorizationTypeDetermined.AddListener(DeterminedAuthorizationType);
        }

        private void OnDestroy()
        {
            keyVault.OnAuthorizationTypeDetermined.RemoveListener(DeterminedAuthorizationType);
        }

        public void UrlHasChanged(string newURL)
        {
            this.url = newURL;
            keyVault.TryToFindSpecificCredentialType(newURL.ToString(), "");
        }

        private void DeterminedAuthorizationType(string url, AuthorizationType authorizationType)
        {
            this.authorizationType = authorizationType;
            hasValidCredentials = authorizationType != AuthorizationType.Unknown;
            Debug.Log("Determined authorization type: " + authorizationType + " for url: " + url, this.gameObject);
            if (hasValidCredentials)
            {
                CredentialsSucceeded?.Invoke(false); //we dont wanto hide this window
            }
        }

        public void ApplyCredentials()
        {
            switch (authorizationType)
            {
                case AuthorizationType.UsernamePassword:
                    SetCredentials(UserName, PasswordOrKeyOrTokenOrCode);
                    keyVault.TryBasicAuthentication(
                        url,
                        UserName,
                        PasswordOrKeyOrTokenOrCode
                    );
                    break;
                case AuthorizationType.InferableSingleKey:
                    keyVault.TryToFindSpecificCredentialType(
                        url,
                        PasswordOrKeyOrTokenOrCode
                    );
                    break;
                case AuthorizationType.BearerToken:
                    SetBearerToken(PasswordOrKeyOrTokenOrCode);
                    keyVault.TryToFindSpecificCredentialType(
                        url,
                        PasswordOrKeyOrTokenOrCode
                    );
                    break;
                case AuthorizationType.Key:
                    SetKey(PasswordOrKeyOrTokenOrCode);
                    keyVault.TryToFindSpecificCredentialType(
                        url,
                        PasswordOrKeyOrTokenOrCode
                    );
                    break;
                case AuthorizationType.Code:
                    SetCode(PasswordOrKeyOrTokenOrCode);
                    keyVault.TryToFindSpecificCredentialType(
                        url,
                        PasswordOrKeyOrTokenOrCode
                    );
                    break;
                case AuthorizationType.Token:
                    SetToken(PasswordOrKeyOrTokenOrCode);
                    keyVault.TryToFindSpecificCredentialType(
                        url,
                        PasswordOrKeyOrTokenOrCode
                    );
                    break;
                case AuthorizationType.Public:
                    ClearCredentials();
                    break;
                case AuthorizationType.Unknown:
                    ClearCredentials();
                    CredentialsAccepted.Invoke(false);
                    break;
            }
        }

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

        public void SetCredentials(string username, string password)
        {
            AddCustomHeader("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(username + ":" + password)), true);           
        }

        public void SetKey(string key)
        {
            personalKey = key;
            publicKey = key;
            QueryKeyName = "key";
        }

        public void SetBearerToken(string token)
        {
            AddCustomHeader("Authorization", "Bearer " + token);
        }

        public void SetCode(string code)
        {
            personalKey = code;
            publicKey = code;
            QueryKeyName = "code";
        }

        public void SetToken(string token)
        {
            personalKey = token;
            publicKey = token;
            QueryKeyName = "token";
        }

        public void ClearCredentials()
        {
            personalKey = "";
            publicKey = "";
            QueryKeyName = "key";
            ClearKeyFromURL();
        }

        /// <summary>
        /// Add custom headers for all internal WebRequests
        /// </summary>
        public void AddCustomHeader(string key, string value, bool replace = true)
        {
            if (replace && customHeaders.ContainsKey(key))
                customHeaders[key] = value;
            else
                customHeaders.Add(key, value);
        }

        public void ClearKeyFromURL()
        {
            if (CredentialQuery != string.Empty)
            {
                tilesetUrl = tilesetUrl.Replace(CredentialQuery, string.Empty);
            }
        }
    }
}