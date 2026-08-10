using System;
using System.Text.RegularExpressions;
using UnityEngine;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.OgcWebServices.Shared;
using Netherlands3D.Twin.Layers.LayerTypes.Credentials.Properties;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Utility;

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
        
        //The following is needed to send a valid GetFeature request to check for credentials. We don't want to waste many resources, so request only 1 feature
        public const string testBBox = "0,300000,280000,625000";
        private static readonly Regex CountRegex = new(@"([?&])count=\d+", RegexOptions.IgnoreCase);

        protected override void OnVisualizationInitialize()
        {
            base.OnVisualizationInitialize();
            CartesianTileWFSLayer.WFSGeoJSONLayer = this;
        }

        protected override void UpdateURL(Uri storedUri)
        {
            string testUrl = storedUri.ToString().Replace("{0}", testBBox);
            // Add or replace count to limit resource usage
            testUrl = CountRegex.IsMatch(testUrl) ? CountRegex.Replace(testUrl, "${1}count=1") : testUrl + "&count=1";

            base.UpdateURL(new Uri(testUrl));
            cartesianTileWFSLayer.WfsUrl = storedUri.ToString();
        }

        protected override void HandleCredentials(Uri uri, StoredAuthorization auth)
        {
            if (auth.GetType() != typeof(Public))//if it is public, we don't want the property panel to show up
            {
                InitProperty<CredentialsRequiredPropertyData>(LayerData.LayerProperties);
            }
            
            if(auth is FailedOrUnsupported)
            {
                cartesianTileWFSLayer.isEnabled = false;
                LayerData.HasValidCredentials = false;
                return;
            }
            
            var getCapabilitiesString = OgcWebServicesUtility.CreateGetCapabilitiesURL(LayerData.GetProperty<LayerURLPropertyData>().Url.ToString(), ServiceType.Wfs);
            var getCapabilitiesUrl = new Uri(getCapabilitiesString);
            BoundingBoxCache.Instance.GetBoundingBoxContainer(
                getCapabilitiesUrl,
                auth,
                (responseText) => new WfsGetCapabilities(getCapabilitiesUrl, responseText), 
                SetBoundingBox
            );
            
            cartesianTileWFSLayer.SetAuthorization(auth);
            LayerData.HasValidCredentials = true;
            cartesianTileWFSLayer.isEnabled = LayerData.ActiveInHierarchy;
            StartLoadingData(uri, auth);
        }
        
        public void SetBoundingBox(BoundingBoxContainer boundingBoxContainer)
        {
            var wfsUrl = LayerData.GetProperty<LayerURLPropertyData>().Url.ToString();
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