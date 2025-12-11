using System;
using System.Text;
using Netherlands3D.Tilekit.WriteModel;

namespace Netherlands3D.Tilekit.TileSetBuilders
{
    public class WmsTileSetBuilder : QuadTreeBuilder
    {
        // String is acceptable, this is a one-time allocation for this service and not on the hot path
        private readonly string wmsMapUrl;

        public WmsTileSetBuilder(string wmsMapUrl)
        {
            this.wmsMapUrl = wmsMapUrl;
        }

        protected override int GenerateUrl(TileSet tileSet, int tileIndex, BoxBoundingVolume boundingVolume)
        {
            // TODO: Use https://github.com/Cysharp/ZString to generate the URL in an alloc free way directly into a span
            var sb = new StringBuilder(wmsMapUrl)
                .AppendFormat(
                    "&bbox={0},{1},{2},{3}", 
                    boundingVolume.TopLeft.x,
                    boundingVolume.TopLeft.y,
                    boundingVolume.BottomRight.x,
                    boundingVolume.BottomRight.y
                );
            
            // copy the string buffer contents to a span to prevent heap allocation
            Span<char> sbBuffer = stackalloc char[sb.Length];
            sb.CopyTo(0, sbBuffer, sb.Length);
            
            // copy the utf8 encoded bytes into a span to prevent heap allocations, and still get a good UTF-8 set of bytes.
            Span<byte> buffer = stackalloc byte[2048];
            
            // note: the numberOfBytes is different from the number of chars - each UTF-8 character can be between 1 and 4 bytes. 
            var numberOfBytes = Encoding.UTF8.GetBytes(sbBuffer, buffer);
            
            return tileSet.Strings.Add(buffer, numberOfBytes);
        }
    }
}