using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.Networking;

namespace Netherlands3D.Twin
{
    public class Tiles3DContentOverlayInspector : OverlayInspector
    {
        [SerializeField] private KeyVault keyVault;

        [Tooltip("The same URL input is used here, as the one used in the property panel")] [SerializeField]
        private Tile3DLayerPropertySection tile3DLayerPropertySection;

        [Tooltip("The same credentials input is used here, as the one used in the property panel")] [SerializeField]
        private CredentialsPropertySection credentialsPropertySection;

        [SerializeField] private RectTransform credentialExplanation;

        private Tile3DLayer layerWithCredentials;
        private AuthorizationType authorizationType = AuthorizationType.Unknown;

        public override void SetReferencedLayer(ReferencedLayer layer)
        {
            base.SetReferencedLayer(layer);

            layerWithCredentials = layer as Tile3DLayer;
            CloseRightProperties(layer.ReferencedProxy);

            tile3DLayerPropertySection.Tile3DLayer = layerWithCredentials;

            layerWithCredentials.OnURLChanged.AddListener(UrlHasChanged);
            layerWithCredentials.OnServerResponseReceived.AddListener(ServerResponseReceived);
        }

        private void CloseRightProperties(LayerNL3DBase layer)
        {
            var layerManager = FindAnyObjectByType<LayerManager>();
            var ui = layerManager.GetLayerUI(layer);
            ui.ToggleProperties(false);
        }

        private void ServerResponseReceived(UnityWebRequest webRequestResult)
        {
            //Hide the credentials section if the server request failed due to a connection error or data processing error
            if (webRequestResult.ReturnedServerError())
            {
                credentialExplanation.gameObject.SetActive(false);
                credentialsPropertySection.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            keyVault.OnAuthorizationTypeDetermined.AddListener(DeterminedAuthorizationType);

            //Hide the credentials section by default. Only activated if we determine the URL needs credentials
            credentialsPropertySection.gameObject.SetActive(false);
            credentialExplanation.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            //If we close the overlay without getting access to the layer we 'cancel' and remove the layer.
            if (authorizationType == AuthorizationType.Unknown || authorizationType == AuthorizationType.InferableSingleKey)
                layerWithCredentials.DestroyLayer();

            layerWithCredentials.OnURLChanged.RemoveListener(UrlHasChanged);
            keyVault.OnAuthorizationTypeDetermined.RemoveListener(DeterminedAuthorizationType);
        }

        private void UrlHasChanged(string newURL)
        {
            keyVault.TryToFindSpecificCredentialType(newURL, "");
        }

        private void DeterminedAuthorizationType(string url, AuthorizationType authorizationType)
        {
            this.authorizationType = authorizationType;
            Debug.Log("Determined authorization type: " + authorizationType + " for url: " + url, this.gameObject);

            // It appears the current url needs authentication/authorization
            switch (authorizationType)
            {
                case AuthorizationType.Public:
                case AuthorizationType.UsernamePassword:
                case AuthorizationType.Key:
                case AuthorizationType.Token:
                case AuthorizationType.Code:
                case AuthorizationType.BearerToken:
                    //We are in. Close overlay wizard.
                    Debug.Log("Close overlay;");
                    CloseOverlay();
                    break;
                case AuthorizationType.InferableSingleKey:
                default:
                    //Something went wrong, show the credentials section, starting with a default authentication input type
                    var startingAuthenticationType = keyVault.GetKnownAuthorizationTypeForURL(url);
                    credentialsPropertySection.LayerWithCredentials = layerWithCredentials;
                    credentialsPropertySection.SetAuthorizationInputType(startingAuthenticationType);
                    credentialsPropertySection.gameObject.SetActive(true);
                    credentialExplanation.gameObject.SetActive(true);
                    break;
            }
        }
    }
}