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
using UnityEngine.Networking;
using Netherlands3D.Twin.Layers.LayerTypes.Credentials;
using Netherlands3D.Credentials;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.DataTypeAdapters;

namespace Netherlands3D.Functionalities.Wms
{
    /// <summary>
    /// Extention of LayerGameObject that injects a 'streaming' dataprovider WMSTileDataLayer
    /// </summary>
    public class WMSLayerGameObject : CartesianTileLayerGameObject, ILayerWithPropertyData, ILayerWithPropertyPanels
    {
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

        public List<IPropertySectionInstantiator> GetPropertySections()
        {
            propertySections = GetComponents<IPropertySectionInstantiator>().ToList();
            return propertySections;
        }

        protected override void Awake()
        {
            base.Awake();
            wmsProjectionLayer = GetComponent<WMSTileDataLayer>();

            credentialHandler = GetComponent<ICredentialHandler>();
            credentialHandler.OnAuthorizationHandled.AddListener(HandleCredentials);

            //we need to resolve the listener to the datatypechain because this is a prefab and it doesnt know about whats present in the scene
            DataTypeChain chain = FindObjectOfType<DataTypeChain>();
            if(chain != null) 
                credentialHandler.CredentialsSucceeded.AddListener(chain.DetermineAdapter);

            LayerData.LayerSelected.AddListener(OnSelectLayer);
            LayerData.LayerDeselected.AddListener(OnDeselectLayer);
        }

        protected override void Start()
        {
            base.Start();
            UpdateURL(urlPropertyData.Data);  
            LayerData.LayerOrderChanged.AddListener(SetRenderOrder);
            SetRenderOrder(LayerData.RootIndex);


            var getCapabilitiesString = OgcWebServicesUtility.CreateGetCapabilitiesURL(wmsProjectionLayer.WmsUrl, ServiceType.Wms);
            var getCapabilitiesUrl = new Uri(getCapabilitiesString);
            Legend.Instance.GetLegendUrl(wmsProjectionLayer.WmsUrl, OnLegendUrlsReceived);
            BoundingBoxCache.Instance.GetBoundingBoxContainer(
                getCapabilitiesUrl,
                (responseText) => new WmsGetCapabilities(getCapabilitiesUrl, responseText),
                SetBoundingBox
            );
        }

        private void OnLegendUrlsReceived(LegendUrlContainer urlContainer)
        {
            SetLegendActive(ShowLegendOnSelect);
        }

        public void SetLegendActive(bool active)
        {
            Legend.Instance.ShowLegend(wmsProjectionLayer.WmsUrl, active);
        }

        //a higher order means rendering over lower indices
        private void SetRenderOrder(int order)
        {
            //we have to flip the value because a lower layer with a higher index needs a lower render index
            WMSProjectionLayer.RenderIndex = -order;
        }

        private void HandleCredentials(StoredAuthorization auth)
        {
            ClearCredentials();

            if (auth is HeaderBasedAuthorization headerBasedAuthorization)
            {
                var (headerName, headerValue) = headerBasedAuthorization.GetHeaderKeyAndValue();
                WMSProjectionLayer.AddCustomHeader(headerName, headerValue, true);
                WMSProjectionLayer.RefreshTiles();
                return;
            }
            
            if (auth is QueryStringAuthorization inferableSingleKey)
            {
                WMSProjectionLayer.AddCustomQueryParameter(inferableSingleKey.queryKeyName, inferableSingleKey.key);
                WMSProjectionLayer.RefreshTiles();
                return;
            }
            
            throw new NotImplementedException("Authorization type " + auth.GetType() + " is not implemented in " + GetType());
        }

        public void ClearCredentials()
        {
            WMSProjectionLayer.ClearCustomHeaders();
            WMSProjectionLayer.ClearCustomQueryParameters();
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

        private void UpdateURL(Uri urlWithoutQuery)
        {
            credentialHandler.BaseUri = urlWithoutQuery; //apply the URL from what is stored in the Project data
            WMSProjectionLayer.WmsUrl = urlWithoutQuery.ToString();
            credentialHandler.ApplyCredentials();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            LayerData.LayerOrderChanged.RemoveListener(SetRenderOrder);
            LayerData.LayerSelected.RemoveListener(OnSelectLayer);
            LayerData.LayerDeselected.RemoveListener(OnDeselectLayer);
        }

        private void OnSelectLayer(LayerData layer)
        {
            SetLegendActive(ShowLegendOnSelect);
        }

        private void OnDeselectLayer(LayerData layer)
        {
            if (!string.IsNullOrEmpty(wmsProjectionLayer.WmsUrl))
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