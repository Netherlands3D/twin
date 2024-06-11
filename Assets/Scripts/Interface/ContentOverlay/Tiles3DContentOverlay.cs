using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Netherlands3D.TileSystem;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.UI.LayerInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace Netherlands3D.Twin
{
    public class Tiles3DContentOverlayInspector : OverlayInspector
    {
        [SerializeField] private KeyVault keyVault;

        [Tooltip("The same URL input is used here, as the one used in the property panel")]
        [SerializeField] private Tile3DLayerPropertySection tile3DLayerPropertySection;

        [Tooltip("The same credentials input is used here, as the one used in the property panel")]
        [SerializeField] private CredentialsPropertySection credentialsPropertySection;
        [SerializeField] private RectTransform credentialExplanation;

        private Tile3DLayer2 layerWithCredentials;
        private AuthorizationType authorizationType = AuthorizationType.Unknown;

        public override void SetReferencedLayer(ReferencedLayer layer)
        {
            base.SetReferencedLayer(layer);

            layerWithCredentials = layer as Tile3DLayer2;

            tile3DLayerPropertySection.Layer = layerWithCredentials;
            credentialsPropertySection.LayerWithCredentials = layerWithCredentials;

            layerWithCredentials.OnURLChanged.AddListener(UrlHasChanged);
        }

        private void OnEnable()
        {
            keyVault.OnAuthorizationTypeDetermined.AddListener(DeterminedAuthorizationType);

            //Hide the credentials section by default. Only activated if we determine the URL needs credentials
            credentialsPropertySection.gameObject.SetActive(false);
            credentialExplanation.gameObject.SetActive(false);
        }

        private void OnDestroy() {
            layerWithCredentials.OnURLChanged.RemoveListener(UrlHasChanged);
            keyVault.OnAuthorizationTypeDetermined.RemoveListener(DeterminedAuthorizationType);
        }

        private void UrlHasChanged(string newURL)
        {
            keyVault.TryToFindSpecificCredentialType(newURL, "");
        }

        public override void CloseOverlay()
        {
            base.CloseOverlay();

            //If we close the overlay with close button without getting access to the layer we 'cancel' and remove the layer.
            if(authorizationType == AuthorizationType.Unknown || authorizationType == AuthorizationType.ToBeDetermined)
                layerWithCredentials.DestroyLayer();
        }

        private void DeterminedAuthorizationType(string url, AuthorizationType authorizationType)
        {
            //Trim trailing ? or & characters from the URL (TODO: should be fixed in 3DTiles package)
            var layerUrl = layerWithCredentials.URL.TrimEnd('?', '&');
            if(url != layerUrl) return;

            this.authorizationType = authorizationType;
            
            Debug.Log("Determined authorization type: " + authorizationType);
            // It appears the current url needs authentication/authorization
            switch(authorizationType)
            {
                case AuthorizationType.Public:
                case AuthorizationType.UsernamePassword:
                case AuthorizationType.Key:
                case AuthorizationType.Token:
                case AuthorizationType.Code:
                    //We are in. Close overlay wizard.
                    Debug.Log("Close overlay;");
                    CloseOverlay();
                    break;
                case AuthorizationType.ToBeDetermined:
                default:
                    //Something went wrong, show the credentials section, starting with default
                    credentialsPropertySection.SetAuthorizationInputType(AuthorizationType.UsernamePassword);
                    credentialsPropertySection.gameObject.SetActive(true);
                    credentialExplanation.gameObject.SetActive(true);
                    break;
            }
        }
    }
}