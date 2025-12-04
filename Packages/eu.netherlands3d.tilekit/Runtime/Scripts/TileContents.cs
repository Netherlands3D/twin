using System.Runtime.CompilerServices;
using Netherlands3D.Tilekit.MemoryManagement;
using Netherlands3D.Tilekit.WriteModel;
using Unity.Collections;

namespace Netherlands3D.Tilekit
{
    /// A typed, allocation-free view over all contents of a tile.
    /// NOTE: This wraps a NativeSlice under the hood; if the underlying NativeList grows,
    /// previously captured views become invalid. Use immediately or seal storage to NativeArray.
    public struct TileContents
    {
        private readonly TileSet store;
        private Bucket<TileContentData> bucket;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TileContents(TileSet store, Bucket<TileContentData> bucket)
        {
            this.store = store;
            this.bucket = bucket;
        }

        public int Count => bucket.Count;

        public TileContent this[int i] => new(store, bucket[i]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<TileContentData>.Enumerator GetEnumerator() => bucket.GetEnumerator();
    }
}