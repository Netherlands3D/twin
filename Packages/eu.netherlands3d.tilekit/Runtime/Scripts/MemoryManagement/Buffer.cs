using System;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace Netherlands3D.Tilekit.MemoryManagement
{
    /// NOTE: Buckets created from NativeList<T> are **transient views** over the list's current buffer.
    /// If the list grows (reallocates), previously created buckets/slices become invalid. Use immediately,
    public class Buffer<T> : IDisposable where T : unmanaged
    {
        private NativeList<BlockRange> ranges;
        public NativeList<BlockRange> Ranges => ranges;
        private NativeList<T> items;
        public NativeList<T> Items => items;
        public int Length => ranges.Length;
        public int Capacity => ranges.Capacity;
        public BufferBlock<T> this[int index] => BufferBlock<T>.From(items, ranges[index]);
        
        public Buffer(int blockCapacity, int itemCapacity, Allocator alloc)
        {
            ranges = new NativeList<BlockRange>(blockCapacity, alloc);
            items = new NativeList<T>(itemCapacity, alloc);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Add(ReadOnlySpan<T> block)
        {
            int idx = ranges.Length;
            int off = items.Length;
            ranges.AddNoResize(new BlockRange(off, block.Length));
            for (int i = 0; i < block.Length; i++) this.items.AddNoResize(block[i]);
            return idx;
        }

        public int Add(ReadOnlySpan<T> block, int length)
        {
            int idx = ranges.Length;
            int off = items.Length;
            ranges.AddNoResize(new BlockRange(off, block.Length));
            for (int i = 0; i < length; i++) this.items.AddNoResize(block[i]);
            return idx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BufferBlock<T> GetBlockById(int rangeIndex) => this[rangeIndex];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            ranges.Clear();
            items.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            Clear();
            ranges.Dispose();
            items.Dispose();
        }
    }
}