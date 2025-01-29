using System;
using UnityEngine;
using Netherlands3D.Twin.Layers.Properties;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Utility;
using Netherlands3D.Web;

namespace Netherlands3D.Functionalities.Wfs
{
    /// <summary>
    /// Extention of GeoJSONLayerGameObject that injects a 'streaming' dataprovider WFSGeoJSONTileDataLayer
    /// </summary>
    public class WFSGeoJsonLayerGameObject : GeoJsonLayerGameObject
    {
        [SerializeField] private WFSGeoJSONTileDataLayer cartesianTileWFSLayer;

        public WFSGeoJSONTileDataLayer CartesianTileWFSLayer => cartesianTileWFSLayer;
        
        protected void Awake()
        {
            CartesianTileWFSLayer.WFSGeoJSONLayer = this;
        }

        protected override void StartLoadingData()
        {
            var wfsUrl = urlPropertyData.Data.ToString();
            cartesianTileWFSLayer.WfsUrl = wfsUrl;
            WFSBoundingBoxLibrary.Instance.GetBoundingBoxContainer(CreateGetCapabilitiesURL(wfsUrl), SetBoundingBox);
        }

        private string CreateGetCapabilitiesURL(string wfsUrl)
        {
            var uri = new Uri(wfsUrl);
            var baseUrl = uri.GetLeftPart(UriPartial.Path);
            return baseUrl + "?request=GetCapabilities&service=WFS";
        }

        public override void LoadProperties(List<LayerPropertyData> properties)
        {
            var urlProperty = (LayerURLPropertyData)properties.FirstOrDefault(p => p is LayerURLPropertyData);
            if (urlProperty != null)
            {
                this.urlPropertyData = urlProperty;
            }
        }

        public void SetBoundingBox(WFSBoundingBoxContainer boundingBoxContainer)
        {
            var wfsUrl = urlPropertyData.Data.ToString();
            var uri = new Uri(wfsUrl);
            var nvc = new NameValueCollection();
            uri.TryParseQueryString(nvc);
            var version = nvc.Get("version");
            var featureLayerName = nvc.Get(WFSRequest.ParameterNameOfTypeNameBasedOnVersion(version)); 
            
            
            if (boundingBoxContainer.LayerBoundingBoxes.ContainsKey(featureLayerName))
            {
                SetBoundingBox(boundingBoxContainer.LayerBoundingBoxes[featureLayerName]);
                return;
            }
            
            SetBoundingBox(boundingBoxContainer.GlobalBoundingBox);
        }        
        
        public void SetBoundingBox(BoundingBox boundingBox)
        {
            Debug.Log($"Feature Bounding box found in WGS84 CRS: BL: {boundingBox.BottomLeft} TR: {boundingBox.TopRight}");
            cartesianTileWFSLayer.BoundingBox = boundingBox;
        }
    }
}