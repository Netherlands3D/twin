using Netherlands3D.Twin.ExtensionMethods;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.Credentials;
using Netherlands3D.Twin.Layers.LayerTypes.Credentials.Properties;
using Netherlands3D.Twin.Layers.UI.HierarchyInspector;
using Netherlands3D.Twin.Tools.UI;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Netherlands3D.Credentials
{
    //this is the handler for the ui flow to check credentials
    //will be active in the layerpanel when trying to add a layer through url
    public class CredentialHandler : MonoBehaviour, ICredentialHandler
    {
        public bool StatusEnabled => false; //no status here because the layer panel will close on authentication
        public string UserName { get; set; }
        public string PasswordOrKeyOrTokenOrCode { get; set; }
        public AuthorizationType AuthorizationType => authorizationType;

        public UnityEvent<bool> HasValidCredentialsChanged = new();
        public UnityEvent<bool> CredentialsAccepted => HasValidCredentialsChanged;

        public bool HasValidCredentials
        {
            get
            {
                return hasValidCredentials;
            }
            set
            {
                hasValidCredentials = value;
                HasValidCredentialsChanged.Invoke(value);
            }
        }

        private bool hasValidCredentials = false;

        private AuthorizationType authorizationType = AuthorizationType.Unknown;

        [Tooltip("KeyVault Scriptable Object")]
        [SerializeField] private KeyVault keyVault;
        private string url;

        private void Awake()
        {
            //were are not using an instantiator for the user interface, so lets set it here
            ICredentialInterface view = GetComponentInChildren<ICredentialInterface>();
            view.Handler = this;
        }

        private void OnEnable()
        {
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
            if(hasValidCredentials)
            {
                //ContentOverlayContainer.Instance.CloseOverlay();
            }
        }

        public void ApplyCredentials()
        {
            switch (authorizationType)
            {
                case AuthorizationType.UsernamePassword:
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
    }
}