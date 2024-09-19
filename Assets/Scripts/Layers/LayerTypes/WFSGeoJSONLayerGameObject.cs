using UnityEngine;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.CartesianTiles;
using System.Collections.Generic;
using System.Linq;

namespace Netherlands3D.Twin.Layers
{
    /// <summary>
    /// Extention of GeoJSONLayerGameObject that injects a 'streaming' dataprovider WFSGeoJSONTileDataLayer
    /// </summary>
    public class WFSGeoJsonLayerGameObject : GeoJsonLayerGameObject
    {
        [SerializeField] private WFSGeoJSONTileDataLayer cartesianTileWFSLayer;
        public WFSGeoJSONTileDataLayer CartesianTileWFSLayer { get => cartesianTileWFSLayer; }

        protected override void Awake() {
            base.Awake();
            
            CartesianTileWFSLayer.WFSGeoJSONLayer = this;
        }

        public void SetURL(string url)
        {
            this.urlPropertyData.url = url;
            CartesianTileWFSLayer.WfsUrl = url;
        }

        public override void LoadProperties(List<LayerPropertyData> properties)
        {
            LoadDefaultValues();

            var urlProperty = (LayerURLPropertyData)properties.FirstOrDefault(p => p is LayerURLPropertyData);
            if (urlProperty != null)
            {
                this.urlPropertyData = urlProperty;
                CartesianTileWFSLayer.WfsUrl = urlProperty.url;
            }
        }
    }
}