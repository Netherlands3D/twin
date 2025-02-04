using System;
using UnityEngine;
using Netherlands3D.Twin.Layers.Properties;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Functionalities.OgcWebServices.Shared;
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

        public WFSGeoJSONTileDataLayer CartesianTileWFSLayer => cartesianTileWFSLayer;
        
        protected void Awake()
        {
            CartesianTileWFSLayer.WFSGeoJSONLayer = this;
        }

        protected override void StartLoadingData()
        {
            var wfsUrl = urlPropertyData.Data.ToString();
            cartesianTileWFSLayer.WfsUrl = wfsUrl;
            var getCapabilitiesString = OgcWebServicesUtility.CreateGetCapabilitiesURL(wfsUrl, "WFS"); //todo not hardcode service
            var getCapabilitiesUrl = new Uri(getCapabilitiesString);
            BoundingBoxCache.Instance.GetBoundingBoxContainer(
                getCapabilitiesUrl,
                (responseText) => new WfsGetCapabilities(getCapabilitiesUrl, responseText), 
                SetBoundingBox
            );
        }
        
        public override void LoadProperties(List<LayerPropertyData> properties)
        {
            var urlProperty = (LayerURLPropertyData)properties.FirstOrDefault(p => p is LayerURLPropertyData);
            if (urlProperty != null)
            {
                this.urlPropertyData = urlProperty;
            }
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
    }
}