using System;
using Unity.Collections;

namespace Netherlands3D.Tilekit.MemoryManagement
{
    /// NOTE: Buckets created from NativeList<T> are **transient views** over the list's current buffer.
    /// If the list grows (reallocates), previously created buckets/slices become invalid. Use immediately,
    public sealed class Buckets<T> : IDisposable where T : unmanaged
    {
        private NativeList<BucketRange> ranges;
        public NativeList<BucketRange> Ranges => ranges;
        private NativeList<T> flat;
        public NativeList<T> Flat => flat;

        public Buckets(int expectedRanges, int expectedItems, Allocator alloc)
        {
            ranges = new NativeList<BucketRange>(expectedRanges, alloc);
            flat = new NativeList<T>(expectedItems, alloc);
        }
        
        public int Add(ReadOnlySpan<T> items)
        {
            int idx = ranges.Length;
            int off = flat.Length;
            ranges.AddNoResize(new BucketRange(off, items.Length));
            for (int i = 0; i < items.Length; i++) flat.AddNoResize(items[i]);
            return idx;
        }
        public Bucket<T> this[int index] => Bucket<T>.From(flat, ranges[index]);

        public Bucket<T> GetBucket(int rangeIndex) => this[rangeIndex];

        public int Length => ranges.Length;
        public int Capacity => ranges.Capacity;

        public void Clear()
        {
            ranges.Clear();
            flat.Clear();
        }

        public void Dispose()
        {
            Clear();
            ranges.Dispose();
            flat.Dispose();
        }
    }
}