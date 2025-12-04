using System.Collections.Generic;
using System.Text;
using Netherlands3D.Tilekit.TileSets;
using Unity.Collections;

namespace Netherlands3D.Tilekit.DataSets
{
    // https://service.pdok.nl/prorail/spoorwegen/wms/v1_0?request=GetCapabilities&service=WMS
    // https://service.pdok.nl/prorail/spoorwegen/wms/v1_0?request=GetMap&service=WMS&version=1.3.0&layers=kilometrering&styles=kilometrering&CRS=EPSG%3a28992&bbox=155000%2c464000%2c156000%2c465000&width=1024&height=1024&format=image%2fpng&transparent=true

    // https://service.pdok.nl/kadaster/kadastralekaart/wms/v5_0?request=GetCapabilities&service=WMS
    // https://service.pdok.nl/kadaster/kadastralekaart/wms/v5_0?request=GetMap&service=WMS&version=1.3.0&layers=OpenbareRuimteNaam&styles=standaard%3aopenbareruimtenaam&CRS=EPSG%3a28992&width=1024&height=1024&format=image%2fpng&transparent=true&bbox=154000%2c462000%2c155000%2c463000

    // https://docs.ogc.org/cs/22-025r4/22-025r4.html#toc31 for implicit tiling inspiration
    public class WmsDataSet : RasterDataSet
    {
        protected override void Initialize()
        {
            Url = "https://service.pdok.nl/prorail/spoorwegen/wms/v1_0?request=GetMap&service=WMS&version=1.3.0&layers={layers}&styles={styles}&CRS=EPSG%3a28992&width=1024&height=1024&format=image%2fpng&transparent=true&bbox={bbox}";
            var urlStringBuilder = new StringBuilder(Url);
            urlStringBuilder.Replace("{layers}", "kilometrering");
            urlStringBuilder.Replace("{styles}", "kilometrering");
            Url = urlStringBuilder.ToString();

            base.Initialize();
        }

        protected override string GetImageUrl(RasterTileSet.Tile tile)
        {
            var boundingVolume = tile.BoundingVolume.AsBox();

            return new StringBuilder(Url)
                .Replace("{bbox}", $"{boundingVolume.TopLeft.x},{boundingVolume.TopLeft.y},{boundingVolume.BottomRight.x},{boundingVolume.BottomRight.y}")
                .ToString();
        }
    }
}