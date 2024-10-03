using UnityEngine;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.CartesianTiles;
using System.Collections.Generic;
using System.Linq;

namespace Netherlands3D.Twin.Layers
{
    /// <summary>
    /// Extention of LayerGameObject that injects a 'streaming' dataprovider WMSGTileDataLayer
    /// </summary>
    public class WMSLayerGameObject : LayerGameObject, ILayerWithPropertyData
    {
        private WMSTileDataLayer wmsProjectionLayer;
        public WMSTileDataLayer WMSProjectionLayer { get => wmsProjectionLayer; }


        protected LayerURLPropertyData urlPropertyData = new();
        LayerPropertyData ILayerWithPropertyData.PropertyData => urlPropertyData;

        protected virtual void Awake() 
        {                       
            wmsProjectionLayer = GetComponent<WMSTileDataLayer>();
        }

        public void SetURL(string url)
        {
            this.urlPropertyData.url = url;
            //CartesianTileWFSLayer.WfsUrl = url;
            //TODO projectionlayer set this url
        }

        public virtual void LoadProperties(List<LayerPropertyData> properties)
        {
            var urlProperty = (LayerURLPropertyData)properties.FirstOrDefault(p => p is LayerURLPropertyData);
            if (urlProperty != null)
            {
                this.urlPropertyData = urlProperty;
                //CartesianTileWFSLayer.WfsUrl = urlProperty.url;
                //TODO projectionlayer set this url
            }
        }
    }
}