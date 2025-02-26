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
    public class CredentialHandler : MonoBehaviour, ICredentialHandler
    {
        //no status here because the layer panel will close on authentication
        public bool StatusEnabled => false; 
        public string UserName { get; set; }
        public string PasswordOrKeyOrTokenOrCode { get; set; }
        public AuthorizationType AuthorizationType => authorizationType;

        //Todo, event maybe not in interface? we dont want to use this event because after importing the panel needs to be closed
        public UnityEvent<bool> CredentialsAccepted => CredentialsAcceptedDoNothing;        
        public UnityEvent<string> CredentialsSucceeded = new();
        public bool HasValidCredentials => false;        
        private UnityEvent<bool> CredentialsAcceptedDoNothing = new();

        private bool hasValidCredentials = false;
        private bool hasAuthorizationType = false;
        private AuthorizationType authorizationType = AuthorizationType.Unknown;
        public string CredentialQuery { get; private set; } = string.Empty;
        public string credentialUrl;
        [SerializeField] private string queryKeyName = "key";
        public string QueryKeyName { get => queryKeyName; set => queryKeyName = value; }

        [Tooltip("KeyVault Scriptable Object")]
        [SerializeField] private KeyVault keyVault;
        private string urlWithoutCredentials;

        private void Awake()
        {
            //were are not using an instantiator for the user interface, so lets set it here
            ICredentialsPropertySection view = GetComponentInChildren<ICredentialsPropertySection>();
            view.Handler = this;

            keyVault.OnAuthorizationTypeDetermined.AddListener(DeterminedAuthorizationType);
        }

        private void OnDestroy()
        {
            keyVault.OnAuthorizationTypeDetermined.RemoveListener(DeterminedAuthorizationType);
        }

        public void UpdateUrl(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                this.urlWithoutCredentials = url;
                UrlHasChanged(url);
            }
        }

        private void UrlHasChanged(string newURL)
        {
            this.urlWithoutCredentials = newURL;
            keyVault.TryToFindSpecificCredentialType(newURL.ToString(), "");
        }

        private void DeterminedAuthorizationType(string url, AuthorizationType authorizationType)
        {
            this.authorizationType = authorizationType;
            //we check if the authorized type is different from unkown. The keyvault webrequests can never return unknown if authorization was valid
            hasAuthorizationType = authorizationType != AuthorizationType.Unknown;

            //Debug.Log("Determined authorization type: " + authorizationType + " for url: " + url, this.gameObject);
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
                    keyVault.TryBasicAuthentication(
                        urlWithoutCredentials,
                        UserName,
                        PasswordOrKeyOrTokenOrCode
                    );
                    break;
                case AuthorizationType.InferableSingleKey:
                    keyVault.TryToFindSpecificCredentialType(
                        urlWithoutCredentials,
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
            if (string.IsNullOrEmpty(urlWithoutCredentials)) return false;

            UriBuilder uriBuilder = new UriBuilder(urlWithoutCredentials);

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