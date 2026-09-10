using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Netherlands3D.Tilekit.MemoryManagement
{
    /// <summary>
    /// Extension methods that provide lightweight, allocation-free memory usage estimates for Unity native containers.
    /// </summary>
    /// <remarks>
    /// These methods report payload-oriented byte estimates based on container capacity/length/count and element sizes.
    /// They do not include allocator bookkeeping overhead and may not account for internal alignment/padding.
    /// </remarks>
    public static class MemorySizeExtensionMethods
    {
        /// <summary>
        /// Returns reserved bytes for a <see cref="NativeArray{T}"/> (length-weighted; arrays have no separate capacity).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetReservedBytes<T>(this in NativeArray<T> array) where T : unmanaged
            => array.IsCreated ? (long)array.Length * UnsafeUtility.SizeOf<T>() : 0;

        /// <summary>
        /// Returns used bytes for a <see cref="NativeArray{T}"/> (length-weighted; arrays have no separate capacity).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetUsedBytes<T>(this in NativeArray<T> array) where T : unmanaged
            => array.GetReservedBytes();

        /// <summary>
        /// Returns reserved bytes for a <see cref="NativeList{T}"/> (capacity-weighted).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetReservedBytes<T>(this in NativeList<T> list) where T : unmanaged 
            => list.IsCreated ? (long)list.Capacity * UnsafeUtility.SizeOf<T>() : 0;

        /// <summary>
        /// Returns used bytes for a <see cref="NativeList{T}"/> (length-weighted).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetUsedBytes<T>(this in NativeList<T> list) where T : unmanaged 
            => list.IsCreated ? (long)list.Length * UnsafeUtility.SizeOf<T>() : 0;

        /// <summary>
        /// Estimates bytes reserved by a <see cref="NativeParallelHashMap{TKey, TValue}"/> internal storage.
        /// </summary>
        /// <remarks>
        /// This estimates entry storage as: <c>Capacity * (sizeof(key) + sizeof(value) + sizeof(int next))</c>
        /// plus bucket storage: <c>bucketCount * sizeof(int)</c>. Does not include allocator bookkeeping, padding, or alignment.
        /// </remarks>
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
        /// Estimates bytes used by a <see cref="NativeParallelHashMap{TKey, TValue}"/> as a payload-weighted measure.
        /// </summary>
        /// <remarks>
        /// Used bytes are estimated as: <c>Count * (sizeof(key) + sizeof(value) + sizeof(int next))</c> plus buckets.
        /// Buckets are included even when count is zero.
        /// </remarks>
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

            return count * entryBytes + (long)bucketCapacity * sizeof(int);
        }
    }
}