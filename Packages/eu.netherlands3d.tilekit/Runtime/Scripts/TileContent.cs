using System.Runtime.CompilerServices;
using Netherlands3D.Tilekit.WriteModel;

namespace Netherlands3D.Tilekit
{
    /// A typed, allocation-free view over a single content entry.
    public readonly struct TileContent
    {
        private readonly TileSet tileSet;
        private readonly TileContentData data;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TileContent(TileSet tileSet, in TileContentData data)
        {
            this.tileSet = tileSet;
            this.data = data;
        }

        public BoundingVolumeRef Bounds => data.BoundingVolume;
        private int UriIndex => data.UriIndex;

        public string Uri()
        {
            return tileSet.ContentUrls.GetAsString(UriIndex);
        }
    }
}