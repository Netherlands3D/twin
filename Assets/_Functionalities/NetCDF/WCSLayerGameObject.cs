using System;
using Netherlands3D.Twin.Layers.Properties;
using System.Collections.Generic;
using Netherlands3D.OgcWebServices.Shared;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using Netherlands3D.Credentials;
using Netherlands3D.Credentials.StoredAuthorization;
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
    public class WCSLayerGameObject : CartesianTileLayerGameObject, IVisualizationWithPropertyData
    {
        private WCSTileDataLayer netCDFLayer;
        public Vector2Int Resolution = Vector2Int.one * 1000;
   
        public override BoundingBox Bounds => netCDFLayer?.BoundingBox;
        
        protected override void OnVisualizationInitialize()
        {
            base.OnVisualizationInitialize();
            netCDFLayer = GetComponent<WCSTileDataLayer>();
        }

        protected override void OnVisualizationReady()
        {
            base.OnVisualizationReady();
            // var urlPropertyData = LayerData.GetProperty<LayerURLPropertyData>();
            // UpdateURL(urlPropertyData.Url);
        }
        
        public virtual void LoadProperties(List<LayerPropertyData> properties)
        {
            var urlPropertyData = LayerData.GetProperty<LayerURLPropertyData>();
            
        }
    }
}