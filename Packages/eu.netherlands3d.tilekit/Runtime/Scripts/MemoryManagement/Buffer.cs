using System;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace Netherlands3D.Tilekit.MemoryManagement
{
    /// NOTE: BufferBlocks created from NativeList<T> are **transient views** over the list's current buffer.
    /// If the list grows (reallocates), previously created buckets/slices become invalid. Use immediately,
    public class Buffer<T> : IDisposable, IMemoryReporter where T : unmanaged
    {
        protected NativeList<BlockRange> Ranges;
        protected NativeList<T> Items;
        public int Length => Ranges.Length;
        public int Capacity => Ranges.Capacity;
        public BufferBlock<T> this[int index] => BufferBlock<T>.From(Items, Ranges[index]);
        
        public Buffer(int blockCapacity, int itemCapacity, Allocator alloc)
        {
            Ranges = new NativeList<BlockRange>(blockCapacity, alloc);
            Items = new NativeList<T>(itemCapacity, alloc);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual int Add(ReadOnlySpan<T> block)
        {
            int idx = Ranges.Length;
            int off = Items.Length;
            Ranges.AddNoResize(new BlockRange(off, block.Length));
            for (int i = 0; i < block.Length; i++) this.Items.AddNoResize(block[i]);
            return idx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long GetReservedBytes() => Ranges.GetReservedBytes() + Items.GetReservedBytes();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long GetUsedBytes() => Ranges.GetUsedBytes() + Items.GetUsedBytes();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BufferBlock<T> GetBlockById(int rangeIndex) => this[rangeIndex];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            Ranges.Clear();
            Items.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            Clear();
            Ranges.Dispose();
            Items.Dispose();
        }
    }
}