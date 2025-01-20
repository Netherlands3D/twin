using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.Networking;

namespace Netherlands3D.Functionalities.OGC3DTiles
{
    public class Tiles3DContentOverlayInspector : OverlayInspector
    {
        [SerializeField] private KeyVault keyVault;

        [Tooltip("The same URL input is used here, as the one used in the property panel")] [SerializeField]
        private Tile3DLayerPropertySection tile3DLayerPropertySection;

        [Tooltip("The same credentials input is used here, as the one used in the property panel")] 
        [SerializeField] private CredentialsInputPropertySection credentialsPropertySection;

        [SerializeField] private RectTransform credentialExplanation;

        private Tile3DLayerGameObject layerGameObjectWithCredentials;
        private AuthorizationType authorizationType = AuthorizationType.Unknown;

        public override void SetReferencedLayer(LayerGameObject layerGameObject)
        {
            base.SetReferencedLayer(layerGameObject);
            credentialsPropertySection.Handler = layerGameObject.GetComponent<LayerCredentialsHandler>();

            layerGameObjectWithCredentials = layerGameObject as Tile3DLayerGameObject;
            CloseRightProperties(layerGameObject.LayerData);

            tile3DLayerPropertySection.Tile3DLayerGameObject = layerGameObjectWithCredentials;
            layerGameObjectWithCredentials.OnURLChanged.AddListener(UrlHasChanged);
            layerGameObjectWithCredentials.OnServerResponseReceived.AddListener(ServerResponseReceived);
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
                layerGameObjectWithCredentials.DestroyLayer();

            layerGameObjectWithCredentials.OnURLChanged.RemoveListener(UrlHasChanged);
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
                    credentialsPropertySection.SetAuthorizationInputType(startingAuthenticationType);
                    credentialsPropertySection.gameObject.SetActive(true);
                    credentialExplanation.gameObject.SetActive(true);
                    break;
            }
        }
    }
}