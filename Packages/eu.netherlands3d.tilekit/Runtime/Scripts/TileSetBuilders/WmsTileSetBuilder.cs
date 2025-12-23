using System;
using System.Text;
using Netherlands3D.Tilekit.WriteModel;
using Unity.Collections;

namespace Netherlands3D.Tilekit.TileSetBuilders
{
    public class WmsTileSetBuilder : QuadTreeBuilder
    {
        // String is acceptable, this is a one-time allocation for this service and not on the hot path
        private readonly string wmsMapUrl;

        public WmsTileSetBuilder(string wmsMapUrl, string layers, string styles, string crs)
        {
            var version = "1.3.0";
            var queryString = new StringBuilder()
                .AppendFormat(
                    "service=WMS&version={0}&request=GetMap&layers={1}&styles={2}&CRS={3}&width=1024&height=1024&format=image%2fpng&transparent=true", 
                    version,
                    layers, 
                    styles, 
                    crs
                )
                .ToString();

            // Replace query string with the one we just built
            this.wmsMapUrl = new UriBuilder(wmsMapUrl) { Query = queryString }.Uri.ToString();
        }

        protected override int GenerateUrl(TileSet tileSet, BoxBoundingVolume boundingVolume)
        {
            // TODO: Use https://github.com/Cysharp/ZString to generate the URL in an alloc free way directly into a span
            var stringBuilder = new StringBuilder(wmsMapUrl)
                .AppendFormat(
                    "&bbox={0},{1},{2},{3}", 
                    boundingVolume.TopLeft.x,
                    boundingVolume.TopLeft.y,
                    boundingVolume.BottomRight.x,
                    boundingVolume.BottomRight.y
                );
            
            return tileSet.ContentUrls.Add(stringBuilder.ToString());
        }
    }
}