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
        private BufferBlock<TileContentData> bufferBlock;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TileContents(TileSet store, BufferBlock<TileContentData> bufferBlock)
        {
            this.store = store;
            this.bufferBlock = bufferBlock;
        }

        public int Count => bufferBlock.Length;

        public TileContent this[int i] => new(store, bufferBlock[i]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<TileContentData>.Enumerator GetEnumerator() => bufferBlock.GetEnumerator();
    }
}