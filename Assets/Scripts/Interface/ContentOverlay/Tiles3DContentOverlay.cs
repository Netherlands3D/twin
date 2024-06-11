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

        private void OnEnable()
        {
            keyVault.OnAuthorizationTypeDetermined.AddListener(DisplayCredentialsInputIfRequired);

            //Hide the credentials section by default. Only activated if we determine the URL needs credentials
            credentialsPropertySection.gameObject.SetActive(false);
            credentialExplanation.gameObject.SetActive(false);
        }

        private void OnDestroy() {
            layerWithCredentials.OnURLChanged.RemoveListener(UrlHasChanged);
            keyVault.OnAuthorizationTypeDetermined.RemoveListener(DisplayCredentialsInputIfRequired);
        }

        private void UrlHasChanged(string newURL)
        {
            keyVault.TryToFindSpecificCredentialType(newURL, "");
        }

        private void DisplayCredentialsInputIfRequired(string url, AuthorizationType authorizationType)
        {
            if(url != layerWithCredentials.URL) return;

            // It appears the current url needs authentication/authorization
            if(authorizationType != AuthorizationType.None)
            {
                credentialsPropertySection.gameObject.SetActive(true);
                credentialExplanation.gameObject.SetActive(true);
            }
        }

        public override void SetReferencedLayer(ReferencedLayer layer)
        {
            base.SetReferencedLayer(layer);

            layerWithCredentials = layer as Tile3DLayer2;

            tile3DLayerPropertySection.Layer = layerWithCredentials;
            credentialsPropertySection.LayerWithCredentials = layerWithCredentials;

            layerWithCredentials.OnURLChanged.AddListener(UrlHasChanged);
        }
    }
}