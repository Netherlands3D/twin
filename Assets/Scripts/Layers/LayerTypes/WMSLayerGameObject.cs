using Netherlands3D.Twin.Layers.Properties;
using System.Collections.Generic;
using System.Linq;

namespace Netherlands3D.Twin.Layers
{
    /// <summary>
    /// Extention of LayerGameObject that injects a 'streaming' dataprovider WMSTileDataLayer
    /// </summary>
    public class WMSLayerGameObject : CartesianTileLayerGameObject, ILayerWithPropertyData
    {
        private WMSTileDataLayer wmsProjectionLayer;
        public WMSTileDataLayer WMSProjectionLayer
        {
            get
            {
                if (wmsProjectionLayer == null)
                    wmsProjectionLayer = GetComponent<WMSTileDataLayer>();
                return wmsProjectionLayer;
            }
        }

        protected LayerURLPropertyData urlPropertyData = new();
        LayerPropertyData ILayerWithPropertyData.PropertyData => urlPropertyData;

        public bool TransparencyEnabled { get => WMSProjectionLayer.TransparencyEnabled; }
        
        protected override void Awake() 
        {
            base.Awake();
            wmsProjectionLayer = GetComponent<WMSTileDataLayer>();            
        }

        public void SetURL(string url)
        {
            this.urlPropertyData.url = url;
            wmsProjectionLayer.WmsUrl = url;
        }

        public virtual void LoadProperties(List<LayerPropertyData> properties)
        {
            var urlProperty = (LayerURLPropertyData)properties.FirstOrDefault(p => p is LayerURLPropertyData);
            if (urlProperty != null)
            {
                this.urlPropertyData = urlProperty;
                wmsProjectionLayer.WmsUrl = urlProperty.url;
            }
        }
    }
}