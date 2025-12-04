using System.Runtime.CompilerServices;
using Netherlands3D.Tilekit.WriteModel;
using Unity.Collections;

namespace Netherlands3D.Tilekit
{
    /// A typed, allocation-free view over a single content entry.
    public readonly struct TileContent
    {
        private readonly TileSet store;
        private readonly TileContentData data;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TileContent(TileSet store, in TileContentData data)
        {
            this.store = store;
            this.data = data;
        }

        public BoundingVolumeRef Bounds => data.BoundingVolume;
        private int UriIndex => data.UriIndex;

        /// Returns false if UriIndex < 0 or string truncated to 127 chars.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetUri(out FixedString128Bytes uri)
        {
            if (UriIndex < 0)
            {
                uri = default;
                return false;
            }

            return store.Strings.TryGetFixedString128(UriIndex, out uri);
        }
    }
}