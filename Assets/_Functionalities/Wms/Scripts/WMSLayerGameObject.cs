using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    /// <summary>
    /// Extention of LayerGameObject that injects a 'streaming' dataprovider WMSTileDataLayer
    /// </summary>
    public class WMSLayerGameObject : CartesianTileLayerGameObject, ILayerWithPropertyData
    {
        public WMSTileDataLayer WMSProjectionLayer
        {
            get
            {
                if (wmsProjectionLayer == null)
                    wmsProjectionLayer = GetComponent<WMSTileDataLayer>();
                return wmsProjectionLayer;
            }
        }
        public LayerPropertyData PropertyData => urlPropertyData;
        public bool TransparencyEnabled { get => WMSProjectionLayer.TransparencyEnabled; }
        public int DefaultEnabledLayersMax { get => WMSProjectionLayer.DefaultEnabledLayersMax; }
        public Vector2Int PreferredImageSize { get => WMSProjectionLayer.PreferredImageSize; }

        private WMSTileDataLayer wmsProjectionLayer;
        private LayerURLPropertyData urlPropertyData;

        protected override void Start()
        {
            base.Start();
            wmsProjectionLayer = GetComponent<WMSTileDataLayer>();
            wmsProjectionLayer.WmsUrl = this.urlPropertyData.url;
            LayerData.RootIndexChanged.AddListener(SetRenderOrder);
            SetRenderOrder(LayerData.RootIndex);
        }

        public void SetUrlPropertyData(string url)
        {
            urlPropertyData = new LayerURLPropertyData();
            urlPropertyData.url = url;
        }

        //a higher order means rendering over lower indices
        public void SetRenderOrder(int order)
        {
            //we have to flip the value because a lower layer with a higher index needs a lower render index
            wmsProjectionLayer.RenderIndex = -order;
        }

        public virtual void LoadProperties(List<LayerPropertyData> properties)
        {
            var urlProperty = (LayerURLPropertyData)properties.FirstOrDefault(p => p is LayerURLPropertyData);
            if (urlProperty != null)
            {
                this.urlPropertyData = urlProperty;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            LayerData.RootIndexChanged.RemoveListener(SetRenderOrder);
        }
    }
}