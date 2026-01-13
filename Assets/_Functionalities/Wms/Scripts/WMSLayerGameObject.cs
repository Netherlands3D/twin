using System;
using Netherlands3D.Twin.Layers.Properties;
using System.Collections.Generic;
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
        private WMSTileDataLayer WMSProjectionLayer => GetAndCacheComponent(ref wmsProjectionLayer);

        private ICredentialHandler credentialHandler;
        private ICredentialHandler CredentialHandler => GetAndCacheComponent(ref credentialHandler);
        
        public bool TransparencyEnabled = true; //this gives the requesting url the extra param to set transparancy enabled by default       
        public int DefaultEnabledLayersMax = 5; //in case the dataset is very large with many layers. lets topggle the layers after this count to not visible.
        public Vector2Int PreferredImageSize = Vector2Int.one * 512;
        public bool ShowLegendOnSelect { get; set; } = true;
        public override BoundingBox Bounds => WMSProjectionLayer?.BoundingBox;
        

        protected override void OnVisualizationReady()
        {
            base.OnVisualizationReady();
            var urlPropertyData = LayerData.GetProperty<LayerURLPropertyData>();
            UpdateURL(urlPropertyData.Url);
            SetRenderOrder(LayerData.RootIndex);
            Legend.Instance.ShowLegend(urlPropertyData.Url.ToString(), ShowLegendOnSelect && LayerData.IsSelected);
        }

        public void SetLegendActive(bool active)
        {
            var urlPropertyData = LayerData.GetProperty<LayerURLPropertyData>();
            if (urlPropertyData.Url == null) return;
            
            Legend.Instance.ShowLegend(urlPropertyData.Url.ToString(), active);
        }

        //a higher order means rendering over lower indices
        private void SetRenderOrder(int order)
        {
            //we have to flip the value because a lower layer with a higher index needs a lower render index
            WMSProjectionLayer.RenderIndex = -order;
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
                WMSProjectionLayer.isEnabled = false;
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

            WMSProjectionLayer.SetAuthorization(auth);
            LayerData.HasValidCredentials = true;           
            WMSProjectionLayer.isEnabled = LayerData.ActiveInHierarchy;
            WMSProjectionLayer.RefreshTiles();
        }

        public void ClearCredentials()
        {
            WMSProjectionLayer.ClearConfig();
        }

        public virtual void LoadProperties(List<LayerPropertyData> properties)
        {
            var urlPropertyData = LayerData.GetProperty<LayerURLPropertyData>();
            if (urlPropertyData == null) return;
            
            UpdateURL(urlPropertyData.Url);
        }

        private void UpdateURL(Uri storedUri)
        {
            if (storedUri == CredentialHandler.Uri && credentialHandler.Authorization != null)
            {
                HandleCredentials(storedUri, credentialHandler.Authorization);
                return;
            }
            
            CredentialHandler.Uri = storedUri; //apply the URL from what is stored in the Project data
            WMSProjectionLayer.WmsUrl = storedUri.ToString();
            CredentialHandler.ApplyCredentials();
        }

        protected override void RegisterEventListeners()
        {
            base.RegisterEventListeners();
            LayerData.LayerOrderChanged.AddListener(SetRenderOrder);
            CredentialHandler.OnAuthorizationHandled.AddListener(HandleCredentials);
            var urlPropertyData = LayerData.GetProperty<LayerURLPropertyData>();
            Legend.Instance.RegisterUrl(urlPropertyData.Url.ToString());
        }

        protected override void UnregisterEventListeners()
        {
            base.UnregisterEventListeners();
            LayerData.LayerOrderChanged.RemoveListener(SetRenderOrder);
            CredentialHandler.OnAuthorizationHandled.RemoveListener(HandleCredentials);
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
            if (isActive)
            {
                UpdateURL(LayerData.GetProperty<LayerURLPropertyData>().Url);
            }

            if (WMSProjectionLayer.isEnabled == isActive) return;

            WMSProjectionLayer.isEnabled = isActive;           
        }

        private void SetBoundingBox(BoundingBoxContainer boundingBoxContainer)
        {
            if (boundingBoxContainer == null) return;

            var urlPropertyData = LayerData.GetProperty<LayerURLPropertyData>();
            var wmsUrl = urlPropertyData.Url.ToString();
            var featureLayerName = OgcWebServicesUtility.GetParameterFromURL(wmsUrl, "layers");

            if (boundingBoxContainer.LayerBoundingBoxes.ContainsKey(featureLayerName))
            {
                WMSProjectionLayer.BoundingBox = boundingBoxContainer.LayerBoundingBoxes[featureLayerName];
                return;
            }

            WMSProjectionLayer.BoundingBox = boundingBoxContainer.GlobalBoundingBox;
        }
    }
}