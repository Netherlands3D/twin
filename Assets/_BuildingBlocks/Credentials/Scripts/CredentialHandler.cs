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

        public UnityEvent<bool> CredentialsAccepted => CredentialsAcceptedSkipped;
        public UnityEvent<bool> CredentialsAcceptedSkipped = new();
        public UnityEvent<string> CredentialsSucceeded = new();
        public bool HasValidCredentials => false;
        private bool hasAuthorizationType = false;
        public string URL { get => url; set => url = value; }

        private bool hasValidCredentials = false;

        private AuthorizationType authorizationType = AuthorizationType.Unknown;

        private Dictionary<string, string> customHeaders = new Dictionary<string, string>();
        public Dictionary<string, string> CustomHeaders { get => customHeaders; private set => customHeaders = value; }

        public string CredentialQuery { get; private set; } = string.Empty;
        public string credentialUrl;
        [SerializeField] private string queryKeyName = "key";
        public string QueryKeyName { get => queryKeyName; set => queryKeyName = value; }

        [Tooltip("KeyVault Scriptable Object")]
        [SerializeField] private KeyVault keyVault;
        private string url;

        private void Awake()
        {
            //were are not using an instantiator for the user interface, so lets set it here
            ICredentialInterface view = GetComponentInChildren<ICredentialInterface>();
            view.Handler = this;

            keyVault.OnAuthorizationTypeDetermined.AddListener(DeterminedAuthorizationType);
            CredentialsAcceptedSkipped.AddListener(SkipCredentialsAccepted);
        }

        private void OnDestroy()
        {
            keyVault.OnAuthorizationTypeDetermined.RemoveListener(DeterminedAuthorizationType);
            CredentialsAcceptedSkipped.RemoveListener(SkipCredentialsAccepted);
        }

        private void SkipCredentialsAccepted(bool accepted)
        {
            
        }

        public void UpdateCredentialUrl(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                this.url = url;
                UrlHasChanged(url);
            }
        }

        public void UrlHasChanged(string newURL)
        {
            this.url = newURL;
            keyVault.TryToFindSpecificCredentialType(newURL.ToString(), "");
        }

        private void DeterminedAuthorizationType(string url, AuthorizationType authorizationType)
        {
            this.authorizationType = authorizationType;
            hasAuthorizationType = authorizationType != AuthorizationType.Unknown;
            Debug.Log("Determined authorization type: " + authorizationType + " for url: " + url, this.gameObject);
            if (hasAuthorizationType)
            {
                if(ConstructURLWithKey())
                    CredentialsSucceeded?.Invoke(credentialUrl);
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
            UserName = username;
            PasswordOrKeyOrTokenOrCode = password;
        }

        public void SetKey(string key)
        {
            PasswordOrKeyOrTokenOrCode = key;
        }

        public void SetBearerToken(string token)
        {
            PasswordOrKeyOrTokenOrCode = token;
        }

        public void SetCode(string code)
        {
            PasswordOrKeyOrTokenOrCode = code;
        }

        public void SetToken(string token)
        {
            PasswordOrKeyOrTokenOrCode = token;
            QueryKeyName = "token";
        }

        public void ClearCredentials()
        {
            PasswordOrKeyOrTokenOrCode = "";
            UserName = "";
            QueryKeyName = "key";
            ClearKeyFromURL();
        }

        public void ClearKeyFromURL()
        {
            if (CredentialQuery != string.Empty)
            {
                credentialUrl = credentialUrl.Replace(CredentialQuery, string.Empty);
            }
        }

        public bool ConstructURLWithKey()
        {
            ClearKeyFromURL(); //remove existing key if any is there
            if (string.IsNullOrEmpty(url)) return false;

            UriBuilder uriBuilder = new UriBuilder(url);

            //Keep an existing query
            if (uriBuilder.Query != null && uriBuilder.Query.Length > 1)
            {
                uriBuilder.Query = uriBuilder.Query.Substring(1) + "&";
            }
            else
            {
                uriBuilder.Query = "";
            }

            if (!string.IsNullOrEmpty(PasswordOrKeyOrTokenOrCode))
            {
                CredentialQuery = $"{QueryKeyName}={PasswordOrKeyOrTokenOrCode}";
                uriBuilder.Query += CredentialQuery;
            }

            credentialUrl = uriBuilder.ToString();
            return !string.IsNullOrEmpty(CredentialQuery);
        }
    }
}