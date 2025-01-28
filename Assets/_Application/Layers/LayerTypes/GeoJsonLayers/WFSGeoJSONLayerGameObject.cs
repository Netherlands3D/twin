using UnityEngine;
using Netherlands3D.Twin.Layers.Properties;
using System.Collections.Generic;
using System.Linq;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers
{
    /// <summary>
    /// Extention of GeoJSONLayerGameObject that injects a 'streaming' dataprovider WFSGeoJSONTileDataLayer
    /// </summary>
    public class WFSGeoJsonLayerGameObject : GeoJsonLayerGameObject
    {
        [SerializeField] private WFSGeoJSONTileDataLayer cartesianTileWFSLayer;
        public WFSGeoJSONTileDataLayer CartesianTileWFSLayer { get => cartesianTileWFSLayer; }

        protected void Awake() 
        {
            CartesianTileWFSLayer.WFSGeoJSONLayer = this;
        }

        protected override void StartLoadingData()
        {
            cartesianTileWFSLayer.WfsUrl = urlPropertyData.Data.ToString();
        }

        public override void LoadProperties(List<LayerPropertyData> properties)
        {
            var urlProperty = (LayerURLPropertyData)properties.FirstOrDefault(p => p is LayerURLPropertyData);
            if (urlProperty != null)
            {
                this.urlPropertyData = urlProperty;
            }
        }

        public void SetBoundingBox(BoundingBox boundingBox)
        {
            cartesianTileWFSLayer.BoundingBox = boundingBox;
        }
    }
}