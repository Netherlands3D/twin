using System;
using Netherlands3D.Twin.Layers.Properties;
using System.Collections.Generic;
using Netherlands3D.OgcWebServices.Shared;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using Netherlands3D.Credentials;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.Functionalities.Wcs;
using Netherlands3D.Functionalities.Wms;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.Credentials.Properties;
using Netherlands3D.Legend;

namespace Netherlands3D.Functionalities.NetCDF
{
    /// <summary>
    /// Extention of LayerGameObject that injects a 'streaming' dataprovider WMSTileDataLayer
    /// </summary>
    [RequireComponent(typeof(WCSTileDataLayer))]
    [RequireComponent(typeof(ICredentialHandler))]
    public class WCSLayerGameObject : CartesianTileLayerGameObject, IVisualizationWithPropertyData
    {
        private WCSTileDataLayer wcsLayer;
        private ICredentialHandler credentialHandler;
        
        public Vector2Int Resolution = Vector2Int.one * 1000;
   
        public override BoundingBox Bounds => wcsLayer?.BoundingBox;
        
        protected override void OnVisualizationInitialize()
        {
            base.OnVisualizationInitialize();
            wcsLayer = GetComponent<WCSTileDataLayer>();
            credentialHandler = GetComponent<ICredentialHandler>();
        }

        protected override void OnVisualizationReady()
        {
            base.OnVisualizationReady();
            var urlPropertyData = LayerData.GetProperty<LayerURLPropertyData>();
            UpdateURL(urlPropertyData.Url);
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
                wcsLayer.isEnabled = false;
                return;
            }
            
            var getCapabilitiesString = OgcWebServicesUtility.CreateGetCapabilitiesURL(wcsLayer.Url, ServiceType.Wcs);
            var getCapabilitiesUrl = new Uri(getCapabilitiesString);
            BoundingBoxCache.Instance.GetBoundingBoxContainer(
                getCapabilitiesUrl,
                auth,
                (responseText) => new WcsGetCapabilities(getCapabilitiesUrl, responseText),
                SetBoundingBox
            );

            wcsLayer.SetAuthorization(auth);
            LayerData.HasValidCredentials = true;           
            wcsLayer.isEnabled = LayerData.ActiveInHierarchy;
            wcsLayer.RefreshTiles();
        }

        public void ClearCredentials()
        {
            wcsLayer.ClearConfig();
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
            wcsLayer.Url = storedUri.ToString();
            credentialHandler.ApplyCredentials();
        }

        protected override void RegisterEventListeners()
        {
            base.RegisterEventListeners();
            credentialHandler.OnAuthorizationHandled.AddListener(HandleCredentials);
        }

        protected override void UnregisterEventListeners()
        {
            base.UnregisterEventListeners();
            credentialHandler.OnAuthorizationHandled.RemoveListener(HandleCredentials);
        }

        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            var urlPropertyData = LayerData.GetProperty<LayerURLPropertyData>();
            if (isActive)
            {
                UpdateURL(urlPropertyData.Url);
            }
            
            if (wcsLayer.isEnabled == isActive) return;

            wcsLayer.isEnabled = isActive;
        }

        private void SetBoundingBox(BoundingBoxContainer boundingBoxContainer)
        {
            if (boundingBoxContainer == null) return;

            var urlPropertyData = LayerData.GetProperty<LayerURLPropertyData>();
            var wcsUrl = urlPropertyData.Url.ToString();

            var coverageName =
                OgcWebServicesUtility.GetParameterFromURL(wcsUrl, "coverageid")
                ?? OgcWebServicesUtility.GetParameterFromURL(wcsUrl, "coverage");

            if (boundingBoxContainer.LayerBoundingBoxes.ContainsKey(coverageName))
            {
                wcsLayer.BoundingBox = boundingBoxContainer.LayerBoundingBoxes[coverageName];
                return;
            }

            wcsLayer.BoundingBox = boundingBoxContainer.GlobalBoundingBox;
        }
    }
}