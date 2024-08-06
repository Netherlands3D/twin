using UnityEngine;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.CartesianTiles;

namespace Netherlands3D.Twin.Layers
{
    /// <summary>
    /// Extention of GeoJSONLayerGameObject that injects a 'streaming' dataprovider.
    /// </summary>
    public class WFSGeoJsonLayerGameObject : GeoJsonLayerGameObject, ILayerWithPropertyData
    {
        [SerializeField] private WFSGeoJSONTileDataLayer cartesianTileWFSLayer;
        public WFSGeoJSONTileDataLayer CartesianTileWFSLayer { get => cartesianTileWFSLayer; }

        private void Awake() {
            CartesianTileWFSLayer.WFSGeoJSONLayer = this;
        }
    }
}