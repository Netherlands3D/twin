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

        public void Replace(NativeArray<T> replacement)
        {
            if (replacement.Length != s.Length)
            {
                throw new System.ArgumentException("Replacing a bucket is only possible if the number of children matches the bucket's size.");
            }

            for (int i = 0; i < replacement.Length; i++)
            {
                s[i] = replacement[i];
            }
        }
    }
}