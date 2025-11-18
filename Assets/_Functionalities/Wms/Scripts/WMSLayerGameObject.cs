using System;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using System.Collections.Generic;
using Netherlands3D.OgcWebServices.Shared;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using UnityEngine.Events;
using Netherlands3D.Credentials;
using Netherlands3D.Credentials.StoredAuthorization;

namespace Netherlands3D.Functionalities.Wms
{
    /// <summary>
    /// Extention of LayerGameObject that injects a 'streaming' dataprovider WMSTileDataLayer
    /// </summary>
    [RequireComponent(typeof(WMSTileDataLayer))]
    [RequireComponent(typeof(ICredentialHandler))]
    public class WMSLayerGameObject : CartesianTileLayerGameObject, IVisualizationWithPropertyData//, ILayerWithPropertyPanels
    {
        public override bool IsMaskable => false;
        private WMSTileDataLayer wmsProjectionLayer;
        private WMSTileDataLayer WMSProjectionLayer => GetAndCacheComponent(ref wmsProjectionLayer);

        private ICredentialHandler credentialHandler;
        private ICredentialHandler CredentialHandler => GetAndCacheComponent(ref credentialHandler);
        
        public bool TransparencyEnabled = true; //this gives the requesting url the extra param to set transparancy enabled by default       
        public int DefaultEnabledLayersMax = 5; //in case the dataset is very large with many layers. lets topggle the layers after this count to not visible.
        public Vector2Int PreferredImageSize = Vector2Int.one * 512;
        public LayerPropertyData PropertyData => URLPropertyData;
        private LayerURLPropertyData URLPropertyData => LayerData.GetProperty<LayerURLPropertyData>();
        public bool ShowLegendOnSelect { get; set; } = true;
        public override BoundingBox Bounds => WMSProjectionLayer?.BoundingBox;

        public UnityEvent<Uri> OnURLChanged => URLPropertyData.OnDataChanged;
        
        protected override void OnLayerInitialize()
        {
            base.OnLayerInitialize();
            CredentialHandler.OnAuthorizationHandled.AddListener(HandleCredentials);
        }

        protected override void OnLayerReady()
        {
            base.OnLayerReady();
            UpdateURL(URLPropertyData.Data);
            LayerData.LayerOrderChanged.AddListener(SetRenderOrder);
            SetRenderOrder(LayerData.RootIndex);
            Legend.Instance.RegisterUrl(URLPropertyData.Data.ToString());
            Legend.Instance.ShowLegend(URLPropertyData.Data.ToString(), ShowLegendOnSelect && LayerData.IsSelected);
        }

        public void SetLegendActive(bool active)
        {
            if (URLPropertyData.Data == null) return;
            
            Legend.Instance.ShowLegend(URLPropertyData.Data.ToString(), active);
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
            WMSProjectionLayer.RefreshTiles();
            WMSProjectionLayer.isEnabled = LayerData.ActiveInHierarchy;
        }

        public void ClearCredentials()
        {
            WMSProjectionLayer.ClearConfig();
        }

        public virtual void LoadProperties(List<LayerPropertyData> properties)
        {
            if (URLPropertyData == null) return;
            
            UpdateURL(URLPropertyData.Data);
        }

        private void UpdateURL(Uri storedUri)
        {
            CredentialHandler.Uri = storedUri; //apply the URL from what is stored in the Project data
            WMSProjectionLayer.WmsUrl = storedUri.ToString();
            CredentialHandler.ApplyCredentials();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            LayerData.LayerOrderChanged.RemoveListener(SetRenderOrder);
            CredentialHandler.OnAuthorizationHandled.RemoveListener(HandleCredentials);
            Legend.Instance.UnregisterUrl(URLPropertyData.Data.ToString());
        }

        public override void OnSelect()
        {
            SetLegendActive(ShowLegendOnSelect);
        }

        public override void OnDeselect()
        {
            SetLegendActive(false);
        }

        private void SetBoundingBox(BoundingBoxContainer boundingBoxContainer)
        {
            if (boundingBoxContainer == null) return;

            var wmsUrl = URLPropertyData.Data.ToString();
            var featureLayerName = OgcWebServicesUtility.GetParameterFromURL(wmsUrl, "layers");

            if (boundingBoxContainer.LayerBoundingBoxes.ContainsKey(featureLayerName))
            {
                WMSProjectionLayer.BoundingBox = boundingBoxContainer.LayerBoundingBoxes[featureLayerName];
                return;
            }

            WMSProjectionLayer.BoundingBox = boundingBoxContainer.GlobalBoundingBox;
        }

        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            if (WMSProjectionLayer.isEnabled == isActive) return;
            
            WMSProjectionLayer.isEnabled = isActive;
        }
    }
}