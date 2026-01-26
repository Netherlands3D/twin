using System;
using Netherlands3D.Twin.Layers.Properties;
using System.Collections.Generic;
using KindMen.Uxios;
using Netherlands3D.OgcWebServices.Shared;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using Netherlands3D.Credentials;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.Credentials.Properties;

namespace Netherlands3D.Functionalities.Wms
{
    /// <summary>
    /// Extention of LayerGameObject that injects a 'streaming' dataprovider WMSTileDataLayer
    /// </summary>
    [RequireComponent(typeof(WMSTileDataLayer))]
    [RequireComponent(typeof(ICredentialHandler))]
    public class WMSLayerGameObject : CartesianTileLayerGameObject, IVisualizationWithPropertyData
    {
        private WMSTileDataLayer wmsProjectionLayer;

        private ICredentialHandler credentialHandler;
        
        public bool TransparencyEnabled = true; //this gives the requesting url the extra param to set transparancy enabled by default       
        public int DefaultEnabledLayersMax = 5; //in case the dataset is very large with many layers. lets topggle the layers after this count to not visible.
        public Vector2Int PreferredImageSize = Vector2Int.one * 512;
        public bool ShowLegendOnSelect { get; set; } = true;
        public override BoundingBox Bounds => wmsProjectionLayer?.BoundingBox;

        protected override void OnVisualizationInitialize()
        {
            base.OnVisualizationInitialize();
            wmsProjectionLayer = GetComponent<WMSTileDataLayer>();
            credentialHandler = GetComponent<ICredentialHandler>();
        }

        protected override void OnVisualizationReady()
        {
            base.OnVisualizationReady();
            var urlPropertyData = LayerData.GetProperty<LayerURLPropertyData>();
            UpdateURL(urlPropertyData.Url);
            SetRenderOrder(LayerData.RootIndex);
            SetLegendActive(ShowLegendOnSelect && LayerData.IsSelected);
        }

        public void SetLegendActive(bool active)
        {
            var urlPropertyData = LayerData.GetProperty<LayerURLPropertyData>();
            if (urlPropertyData.Url == null) return;

            if (!LayerData.HasValidCredentials)
                active = false;
            
            Legend.Instance.ShowLegend(urlPropertyData.Url.ToString(), active);
        }

        //a higher order means rendering over lower indices
        private void SetRenderOrder(int order)
        {
            //we have to flip the value because a lower layer with a higher index needs a lower render index
            wmsProjectionLayer.RenderIndex = -order;
        }

        private void HandleCredentials(Uri uri, StoredAuthorization auth)
        {
            ClearCredentials();

            if (auth.GetType() != typeof(Public))//if it is public, we don't want the property panel to show up
            {
                InitProperty<CredentialsRequiredPropertyData>(LayerData.LayerProperties);
            }
            
            if (auth is FailedOrUnsupported)
            {
                LayerData.HasValidCredentials = false;
                wmsProjectionLayer.isEnabled = false;
                return;
            }

            var getCapabilitiesString = OgcWebServicesUtility.CreateGetCapabilitiesURL(wmsProjectionLayer.WmsUrl, ServiceType.Wms);
            var getCapabilitiesUrl = new Uri(getCapabilitiesString);
            BoundingBoxCache.Instance.GetBoundingBoxContainer(
                getCapabilitiesUrl,
                auth,
                (responseText) => new WmsGetCapabilities(getCapabilitiesUrl, responseText),
                SetBoundingBox
            );

            wmsProjectionLayer.SetAuthorization(auth);
            LayerData.HasValidCredentials = true;           
            wmsProjectionLayer.isEnabled = LayerData.ActiveInHierarchy;
            wmsProjectionLayer.RefreshTiles();
            
            //TODO We need this for now because the child layers are destroyed and readded to keep selection after credential validation, this needs to be removed after https://gemeente-amsterdam.atlassian.net/browse/S3DA-1935
            if(LayerData.IsSelected)
                LayerData.SelectLayer();
        }

        public void ClearCredentials()
        {
            wmsProjectionLayer.ClearConfig();
        }

        public virtual void LoadProperties(List<LayerPropertyData> properties)
        {
            var urlPropertyData = LayerData.GetProperty<LayerURLPropertyData>();
            if (urlPropertyData == null) return;
            
            UpdateURL(urlPropertyData.Url);
        }

        private void UpdateURL(Uri storedUri)
        {
            if (storedUri == credentialHandler.Uri && credentialHandler.Authorization != null)
            {
                HandleCredentials(storedUri, credentialHandler.Authorization);
                return;
            }
            
            credentialHandler.Uri = storedUri; //apply the URL from what is stored in the Project data
            wmsProjectionLayer.WmsUrl = storedUri.ToString();
            credentialHandler.ApplyCredentials();
        }

        protected override void RegisterEventListeners()
        {
            base.RegisterEventListeners();
            LayerData.LayerOrderChanged.AddListener(SetRenderOrder);
            credentialHandler.OnAuthorizationHandled.AddListener(HandleCredentials);
            var urlPropertyData = LayerData.GetProperty<LayerURLPropertyData>();
            Legend.Instance.RegisterUrl(urlPropertyData.Url.ToString(), LayerData.ActiveSelf);
        }

        protected override void UnregisterEventListeners()
        {
            base.UnregisterEventListeners();
            LayerData.LayerOrderChanged.RemoveListener(SetRenderOrder);
            credentialHandler.OnAuthorizationHandled.RemoveListener(HandleCredentials);
            var urlPropertyData = LayerData.GetProperty<LayerURLPropertyData>();
            Legend.Instance.UnregisterUrl(urlPropertyData.Url.ToString());
        }

        public override void OnSelect(LayerData layer)
        {
            SetLegendActive(ShowLegendOnSelect);
        }

        public override void OnDeselect(LayerData layer)
        {
            SetLegendActive(false);
        }

        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            var urlPropertyData = LayerData.GetProperty<LayerURLPropertyData>();
            if (isActive)
            {
                UpdateURL(urlPropertyData.Url);
            }
            
            //we need to parse the layertype from the getmap request url
            var wmsUrl = urlPropertyData.Url.ToString();
            if(OgcWebServicesUtility.GetLayerNameFromURL(wmsUrl, out var layerName))
                Legend.Instance.ToggleLayer(layerName, isActive);
            
            if (wmsProjectionLayer.isEnabled == isActive) return;

            wmsProjectionLayer.isEnabled = isActive;
        }

        private void SetBoundingBox(BoundingBoxContainer boundingBoxContainer)
        {
            if (boundingBoxContainer == null) return;

            var urlPropertyData = LayerData.GetProperty<LayerURLPropertyData>();
            var wmsUrl = urlPropertyData.Url.ToString();
            var featureLayerName = OgcWebServicesUtility.GetParameterFromURL(wmsUrl, "layers");

            if (boundingBoxContainer.LayerBoundingBoxes.ContainsKey(featureLayerName))
            {
                wmsProjectionLayer.BoundingBox = boundingBoxContainer.LayerBoundingBoxes[featureLayerName];
                return;
            }

            wmsProjectionLayer.BoundingBox = boundingBoxContainer.GlobalBoundingBox;
        }
    }
}