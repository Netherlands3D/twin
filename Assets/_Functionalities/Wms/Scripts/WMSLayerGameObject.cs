using System;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using System.Collections.Generic;
using System.Linq;
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
    public class WMSLayerGameObject : CartesianTileLayerGameObject, ILayerWithPropertyData, ILayerWithPropertyPanels
    {
        public override bool IsMaskable => false;
        public WMSTileDataLayer WMSProjectionLayer => wmsProjectionLayer;
        public bool TransparencyEnabled = true; //this gives the requesting url the extra param to set transparancy enabled by default       
        public int DefaultEnabledLayersMax = 5; //in case the dataset is very large with many layers. lets topggle the layers after this count to not visible.
        public Vector2Int PreferredImageSize = Vector2Int.one * 512;
        public LayerPropertyData PropertyData => urlPropertyData;

        private WMSTileDataLayer wmsProjectionLayer;
        protected LayerURLPropertyData urlPropertyData = new();
        public bool ShowLegendOnSelect { get; set; } = true;
        public override BoundingBox Bounds => wmsProjectionLayer?.BoundingBox;

        public UnityEvent<Uri> OnURLChanged => urlPropertyData.OnDataChanged;

        private List<IPropertySectionInstantiator> propertySections = new();

        private ICredentialHandler credentialHandler;

        public new List<IPropertySectionInstantiator> GetPropertySections()
        {
            propertySections = GetComponents<IPropertySectionInstantiator>().ToList();
            return propertySections;
        }

        protected override void OnLayerInitialize()
        {
            wmsProjectionLayer = GetComponent<WMSTileDataLayer>();

            credentialHandler = GetComponent<ICredentialHandler>();
            credentialHandler.OnAuthorizationHandled.AddListener(HandleCredentials);
        }

        protected override void OnLayerReady()
        {
            UpdateURL(urlPropertyData.Data);
            LayerData.LayerOrderChanged.AddListener(SetRenderOrder);
            SetRenderOrder(LayerData.RootIndex);
            Legend.Instance.RegisterUrl(urlPropertyData.Data.ToString());
            Legend.Instance.ShowLegend(urlPropertyData.Data.ToString(), ShowLegendOnSelect && LayerData.IsSelected);
        }

        public void SetLegendActive(bool active)
        {
            if(urlPropertyData.Data != null)
                Legend.Instance.ShowLegend(urlPropertyData.Data.ToString(), active);
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
            var urlProperty = (LayerURLPropertyData)properties.FirstOrDefault(p => p is LayerURLPropertyData);
            if (urlProperty != null)
            {
                this.urlPropertyData = urlProperty;
                UpdateURL(urlPropertyData.Data);
            }
        }

        private void UpdateURL(Uri storedUri)
        {
            credentialHandler.Uri = storedUri; //apply the URL from what is stored in the Project data
            WMSProjectionLayer.WmsUrl = storedUri.ToString();
            credentialHandler.ApplyCredentials();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            LayerData.LayerOrderChanged.RemoveListener(SetRenderOrder);
            credentialHandler.OnAuthorizationHandled.RemoveListener(HandleCredentials);
            Legend.Instance.UnregisterUrl(urlPropertyData.Data.ToString());
        }

        public override void OnSelect()
        {
            SetLegendActive(ShowLegendOnSelect);
        }

        public override void OnDeselect()
        {
            SetLegendActive(false);
        }

        public void SetBoundingBox(BoundingBoxContainer boundingBoxContainer)
        {
            if (boundingBoxContainer == null) return;

            var wmsUrl = urlPropertyData.Data.ToString();
            var featureLayerName = OgcWebServicesUtility.GetParameterFromURL(wmsUrl, "layers");

            if (boundingBoxContainer.LayerBoundingBoxes.ContainsKey(featureLayerName))
            {
                SetBoundingBox(boundingBoxContainer.LayerBoundingBoxes[featureLayerName]);
                return;
            }

            SetBoundingBox(boundingBoxContainer.GlobalBoundingBox);
        }

        public void SetBoundingBox(BoundingBox boundingBox)
        {
            wmsProjectionLayer.BoundingBox = boundingBox;
        }

        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            if (wmsProjectionLayer.isEnabled != isActive)
                wmsProjectionLayer.isEnabled = isActive;
        }
    }
}