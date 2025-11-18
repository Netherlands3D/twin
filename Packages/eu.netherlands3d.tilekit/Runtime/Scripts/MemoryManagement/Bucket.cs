using System.Runtime.CompilerServices;
using Unity.Collections;

namespace Netherlands3D.Tilekit.MemoryManagement
{
    public struct Bucket<T> where T : unmanaged
    {
        private NativeSlice<T> s;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Bucket(NativeSlice<T> slice)
        {
            s = slice;
        }

        public T this[int index] => s[index];
        public int Count => s.Length;

        public NativeSlice<T>.Enumerator GetEnumerator() => s.GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bucket<T> From(NativeArray<T> flat, BucketRange r)
            => new(new NativeSlice<T>(flat, r.Offset, r.Count));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bucket<T> From(NativeList<T> flat, BucketRange r) 
            => new(new NativeSlice<T>(flat.AsArray(), r.Offset, r.Count));
    }
}