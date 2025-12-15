using System.Collections.Generic;
using Netherlands3D.Tilekit.TileSetBuilders;
using UnityEngine;

namespace Netherlands3D.Tilekit.DataSets
{
    public class WmsDataSet : RasterDataSet
    {
        [Tooltip("List of layers to fetch.")]
        public List<string> Layers = new();

        [Tooltip("Styles to apply to the layers.")]
        public List<string> Styles = new();
        
        // TODO: Support multiple CRSs as soon as the Tilekit core knows how to handle them
        private const string crs = "EPSG:28992";
        
        protected override QuadTreeBuilder CreateTileSetBuilder() => new WmsTileSetBuilder(Url, string.Join(',', Layers), string.Join(',', Styles), crs);
    }
}