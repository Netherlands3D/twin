using System;
using System.Runtime.CompilerServices;
using Netherlands3D.Tilekit.MemoryManagement;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Profiling;

namespace Netherlands3D.Tilekit.WriteModel
{
    /// <summary>
    /// The central storage location for memory related to a tileset
    /// </summary>
    public sealed class ColdStorage : IDisposable
    {
        public BoxBoundingVolume AreaOfInterest;
        
        public BoundingVolumeStore BoundingVolumes;
        public NativeList<double> GeometricError; // hot
        public NativeList<MethodOfRefinement> Refine; // hot/small
        public NativeList<SubdivisionScheme> Subdivision; // small
        public NativeList<float4x4> Transform; // consider sparsifying if many are identity

        public readonly Buckets<int> Children;
        public readonly Buckets<TileContentData> Contents;
        public readonly StringTable Strings;
        public Tile Root => Get(0);

        // Allocation and growth are aligned to 64 tiles. This ensures memory alignment 
        // and reduces fragmentation when resizing or replacing storages. Because each 
        // storage grows in fixed-size increments, freed blocks have predictable sizes 
        // and can be efficiently reused by the allocator.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsMultipleOf64(int value) => (value & 63) == 0;

        private static readonly ProfilerCounterValue<int> LayersCounter = new(
            ProfilerCategory.Scripts, 
            "Tilekit - Number of layers",
            ProfilerMarkerDataUnit.Count, 
            ProfilerCounterOptions.FlushOnEndOfFrame
        );

        private static readonly ProfilerCounterValue<int> AllocatedTilesCounter = new(
            ProfilerCategory.Scripts, 
            "Tilekit - Allocated tiles",
            ProfilerMarkerDataUnit.Count, 
            ProfilerCounterOptions.FlushOnEndOfFrame
        );

        private static readonly ProfilerCounterValue<int> ActualTilesCounter = new(
            ProfilerCategory.Scripts, 
            "Tilekit - Actual tiles",
            ProfilerMarkerDataUnit.Count, 
            ProfilerCounterOptions.FlushOnEndOfFrame
        );
        
        static ColdStorage()
        {
            // Always reset counters to 0 on first class use - in case something didn't get disposed correctly
            LayersCounter.Value = 0;
            ActualTilesCounter.Value = 0;
            AllocatedTilesCounter.Value = 0;
        }

        public ColdStorage(BoxBoundingVolume areaOfInterest, int initialSize = 64, Allocator alloc = Allocator.Persistent)
        {
            if (!IsMultipleOf64(initialSize))
            {
                throw new ArgumentException("Initial size must be a multiple of 64", nameof(initialSize));
            }

            AreaOfInterest = areaOfInterest;
            LayersCounter.Value += 1;
            AllocatedTilesCounter.Value += initialSize;
            
            BoundingVolumes = new BoundingVolumeStore();
            BoundingVolumes.Alloc(initialSize, alloc);
            GeometricError = new NativeList<double>(initialSize, alloc);
            Refine = new NativeList<MethodOfRefinement>(initialSize, alloc);
            Subdivision = new NativeList<SubdivisionScheme>(initialSize, alloc);
            Transform = new NativeList<float4x4>(initialSize, alloc);

            // Assume 4 children per tile and have the list autogrow. This matches the concept of quad trees, and
            // even though these should be defined as implicit tilesets - it is a useful metric.
            Children = new Buckets<int>(initialSize, initialSize * 4, alloc);

            // Assume that tiles have a single content by default, there could be multiple but generally there is only 1
            Contents = new Buckets<TileContentData>(initialSize, initialSize, alloc);

            // Assume strings have a length of 128 bytes on average
            Strings = new StringTable(initialSize, initialSize * 128, alloc);
        }

        public int AddTile(
            in BoxBoundingVolume boundingVolume,
            double geometricError,
            ReadOnlySpan<TileContentData> contents,
            ReadOnlySpan<int> children = default,
            MethodOfRefinement refine = MethodOfRefinement.Replace,
            SubdivisionScheme subdivision = SubdivisionScheme.None,
            in float4x4 transform = default
        ) {
            // Take any of the arrays whose length matches the number of tiles in this storage and use it's length
            // as the new id as this is last id + 1
            int id = GeometricError.Length;

            ActualTilesCounter.Value += 1;
            
            BoundingVolumes.Add(id, boundingVolume);
            GeometricError.AddNoResize(geometricError);
            Refine.AddNoResize(refine);
            Subdivision.AddNoResize(subdivision);
            Transform.AddNoResize(transform);

            Children.Add(children);
            Contents.Add(contents);

            return id;
        }

        public Tile Get(int i)
        {
            return new Tile(this, i);
        }

        public void Dispose()
        {
            // Clear all storages first to allow for a proper cleanup before disposing
            Clear();
            
            LayersCounter.Value -= 1;
            AllocatedTilesCounter.Value -= GeometricError.Capacity;
            
            GeometricError.Dispose();
            Refine.Dispose();
            Subdivision.Dispose();
            Transform.Dispose();

            Children.Dispose();
            Contents.Dispose();
            Strings.Dispose();
        }

        public void Clear()
        {
            // Clearing will not free memory, but will reset the counters for the actual tiles - allocated tiles will stay the same
            ActualTilesCounter.Value -= GeometricError.Length;

            GeometricError.Clear();
            Refine.Clear();
            Subdivision.Clear();
            Transform.Clear();

            Children.Clear();
            Contents.Clear();
            Strings.Clear();
        }
    }
}