using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Netherlands3D.Tilekit.MemoryManagement
{
    /// <summary>
    /// Extension methods for Unity containers to provide memory reporting capabilities for use in IMemoryReporter
    /// services.
    /// </summary>
    public static class MemorySizeExtensionMethods
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetReservedBytes<T>(this in NativeArray<T> array) where T : unmanaged
            => array.IsCreated ? (long)array.Length * UnsafeUtility.SizeOf<T>() : 0;

        // NativeArray has no separate "used" vs "reserved" — length is capacity.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetUsedBytes<T>(this in NativeArray<T> array) where T : unmanaged
            => array.IsCreated ? (long)array.Length * UnsafeUtility.SizeOf<T>() : 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetReservedBytes<T>(this in NativeList<T> list) where T : unmanaged 
            => list.IsCreated ? (long)list.Capacity * UnsafeUtility.SizeOf<T>() : 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetUsedBytes<T>(this in NativeList<T> list) where T : unmanaged 
            => list.IsCreated ? (long)list.Length * UnsafeUtility.SizeOf<T>() : 0;

        /// <summary>
        /// Estimated bytes reserved by the map's internal buffers (keys/values/next + buckets).
        /// Does NOT include allocator bookkeeping overhead.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetReservedBytes<TKey, TValue>(this in NativeParallelHashMap<TKey, TValue> map)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            if (!map.IsCreated) return 0;

            int cap = map.Capacity;
            long entryBytes = UnsafeUtility.SizeOf<TKey>() + UnsafeUtility.SizeOf<TValue>() + sizeof(int); // + next

            var bucketData = map.GetUnsafeBucketData();
            int bucketCapacity = bucketData.bucketCapacityMask + 1;

            return (long)cap * entryBytes + (long)bucketCapacity * sizeof(int);
        }

        /// <summary>
        /// "Used" bytes as payload-weighted estimate:
        /// Count * (key + value + next) + buckets.
        /// Buckets always exist, even if Count = 0.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetUsedBytes<TKey, TValue>(this in NativeParallelHashMap<TKey, TValue> map)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            if (!map.IsCreated) return 0;

            int count = map.Count();
            long entryBytes = UnsafeUtility.SizeOf<TKey>() + UnsafeUtility.SizeOf<TValue>() + sizeof(int);

            var bucketData = map.GetUnsafeBucketData();
            int bucketCapacity = bucketData.bucketCapacityMask + 1;

            return (long)count * entryBytes + (long)bucketCapacity * sizeof(int);
        }
    }
}