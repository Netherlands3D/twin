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

        public UnityEvent<bool> CredentialsAccepted => throw new NotImplementedException();

        public bool HasValidCredentials => throw new NotImplementedException();

        private AuthorizationType authorizationType = AuthorizationType.Unknown;

        [Tooltip("KeyVault Scriptable Object")]
        [SerializeField] private KeyVault keyVault;

        private void Awake()
        {
            //were are not using an instantiator for the user interface, so lets set it here
            ICredentialInterface view = GetComponentInChildren<ICredentialInterface>();
            view.Handler = this;
        }

        private void CloseRightProperties(LayerData layer)
        {
            var layerManager = FindAnyObjectByType<LayerUIManager>();
            var ui = layerManager.GetLayerUI(layer);
            ui.ToggleProperties(false);
        }

        private void ServerResponseReceived(UnityWebRequest webRequestResult)
        {
            //Hide the credentials section if the server request failed due to a connection error or data processing error
            if (webRequestResult.ReturnedServerError())
            {
                //credentialExplanation.gameObject.SetActive(false);
                //credentialsPropertySection.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            keyVault.OnAuthorizationTypeDetermined.AddListener(DeterminedAuthorizationType);

            //Hide the credentials section by default. Only activated if we determine the URL needs credentials
            //credentialsPropertySection.gameObject.SetActive(false);
            //credentialExplanation.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {            
            //credentialObject.OnURLChanged.RemoveListener(UrlHasChanged);
            keyVault.OnAuthorizationTypeDetermined.RemoveListener(DeterminedAuthorizationType);
        }

        private void UrlHasChanged(Uri newURL)
        {
            keyVault.TryToFindSpecificCredentialType(newURL.ToString(), "");
        }

        private void DeterminedAuthorizationType(string url, AuthorizationType authorizationType)
        {
            this.authorizationType = authorizationType;
            //Debug.Log("Determined authorization type: " + authorizationType + " for url: " + url, this.gameObject);

            //// It appears the current url needs authentication/authorization
            //switch (authorizationType)
            //{
            //    case AuthorizationType.Public:
            //    case AuthorizationType.UsernamePassword:
            //    case AuthorizationType.Key:
            //    case AuthorizationType.Token:
            //    case AuthorizationType.Code:
            //    case AuthorizationType.BearerToken:
            //        //We are in. Close overlay wizard.
            //        Debug.Log("Close overlay;");
            //        CloseOverlay();
            //        break;
            //    case AuthorizationType.InferableSingleKey:
            //    default:
            //        //Something went wrong, show the credentials section, starting with a default authentication input type
            //        var startingAuthenticationType = keyVault.GetKnownAuthorizationTypeForURL(url);
            //        credentialsPropertySection.SetAuthorizationInputType(startingAuthenticationType);
            //        credentialsPropertySection.gameObject.SetActive(true);
            //        credentialExplanation.gameObject.SetActive(true);
            //        break;
            //}
        }

        public void ApplyCredentials()
        {
            throw new NotImplementedException();
        }

        public void SetAuthorizationInputType(AuthorizationType type)
        {
            throw new NotImplementedException();
        }
    }
}