using System;
using UnityEngine;
using Netherlands3D.Twin.Layers.Properties;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.OgcWebServices.Shared;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Utility;
using Netherlands3D.Credentials;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Credentials.StoredAuthorization;

namespace Netherlands3D.Functionalities.Wfs
{
    /// <summary>
    /// Extention of GeoJSONLayerGameObject that injects a 'streaming' dataprovider WFSGeoJSONTileDataLayer
    /// </summary>
    public class WFSGeoJsonLayerGameObject : GeoJsonLayerGameObject
    {
        [SerializeField] private WFSGeoJSONTileDataLayer cartesianTileWFSLayer;
        public override BoundingBox Bounds => cartesianTileWFSLayer?.BoundingBox;

        public WFSGeoJSONTileDataLayer CartesianTileWFSLayer => cartesianTileWFSLayer;

        private ICredentialHandler credentialHandler; //not using the base as it should probably not even inherit from geojsonlayergameobject

        protected void Awake()
        {
            CartesianTileWFSLayer.WFSGeoJSONLayer = this;

            credentialHandler = GetComponent<ICredentialHandler>();
            credentialHandler.OnAuthorizationHandled.AddListener(HandleCredentials);

            //we need to resolve the listener to the datatypechain because this is a prefab and it doesnt know about what is present in the scene
            DataTypeChain chain = FindObjectOfType<DataTypeChain>();
            if (chain != null)
                credentialHandler.CredentialsSucceeded.AddListener(chain.DetermineAdapter);
        }

        protected override void StartLoadingData()
        {
            var wfsUrl = urlPropertyData.Data.ToString();
            cartesianTileWFSLayer.WfsUrl = wfsUrl;
            var getCapabilitiesString = OgcWebServicesUtility.CreateGetCapabilitiesURL(wfsUrl, ServiceType.Wfs);
            var getCapabilitiesUrl = new Uri(getCapabilitiesString);
            BoundingBoxCache.Instance.GetBoundingBoxContainer(
                getCapabilitiesUrl,
                (responseText) => new WfsGetCapabilities(getCapabilitiesUrl, responseText), 
                SetBoundingBox
            );
        }

        private void HandleCredentials(StoredAuthorization auth)
        {
            ClearCredentials();

            if (auth is BearerToken bearerToken) //todo: moet BearerToken inheriten van InferableSingle key of niet?
            {
                cartesianTileWFSLayer.AddCustomHeader("Authorization", "Bearer " + bearerToken.key);
                cartesianTileWFSLayer.RefreshTiles();
            }
            else if (auth is InferableSingleKey inferableSingleKey)
            {
                cartesianTileWFSLayer.AddCustomQueryParameter(inferableSingleKey.queryKeyName, inferableSingleKey.key);
                cartesianTileWFSLayer.RefreshTiles();
            }
            else if (auth is UsernamePassword usernamePassword)
            {
                cartesianTileWFSLayer.AddCustomHeader("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(usernamePassword.username + ":" + usernamePassword.password)), true);
                cartesianTileWFSLayer.RefreshTiles();
            }
        }

        public override void ClearCredentials()
        {
            cartesianTileWFSLayer.ClearCustomHeaders();
            cartesianTileWFSLayer.ClearCustomQueryParameters();
        }

        public override void LoadProperties(List<LayerPropertyData> properties)
        {
            var urlProperty = (LayerURLPropertyData)properties.FirstOrDefault(p => p is LayerURLPropertyData);
            if (urlProperty != null)
            {
                this.urlPropertyData = urlProperty;
                UpdateURL(urlPropertyData.Data);
            }
        }

        private void UpdateURL(Uri urlWithoutQuery)
        {
            credentialHandler.BaseUri = urlWithoutQuery; //apply the URL from what is stored in the Project data
            cartesianTileWFSLayer.WfsUrl = urlWithoutQuery.ToString();
            credentialHandler.ApplyCredentials();
        }

        public void SetBoundingBox(BoundingBoxContainer boundingBoxContainer)
        {
            var wfsUrl = urlPropertyData.Data.ToString();
            var featureLayerName = WfsGetCapabilities.GetLayerNameFromURL(wfsUrl);
            
            if (boundingBoxContainer.LayerBoundingBoxes.ContainsKey(featureLayerName))
            {
                SetBoundingBox(boundingBoxContainer.LayerBoundingBoxes[featureLayerName]);
                return;
            }
            
            SetBoundingBox(boundingBoxContainer.GlobalBoundingBox);
        }        
        
        public void SetBoundingBox(BoundingBox boundingBox)
        {
            cartesianTileWFSLayer.BoundingBox = boundingBox;
        }
        
        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            if (cartesianTileWFSLayer.isEnabled != isActive)
                cartesianTileWFSLayer.isEnabled = isActive;
        }
    }
}