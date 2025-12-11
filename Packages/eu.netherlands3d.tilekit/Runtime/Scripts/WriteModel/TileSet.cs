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
    public class TileSet : IDisposable
    {
        public Tile Root => GetTile(0);
        private int NumberOfTiles => GeometricError.Length;
        private int NextTileIndex => GeometricError.Length;

        public BoxBoundingVolume AreaOfInterest;
        
        public BoundingVolumeStore BoundingVolumes;
        public NativeList<double> GeometricError; // hot
        public NativeList<MethodOfRefinement> Refine; // hot/small
        public NativeList<float4x4> Transform; // consider sparsifying if many are identity

        public readonly Buffer<int> Children;
        public readonly Buffer<TileContentData> Contents;
        public readonly StringTable Strings;

        public NativeList<int> Warm;
        public NativeList<int> Hot;

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
        
        static TileSet()
        {
            // Always reset counters to 0 on first class use - in case something didn't get disposed correctly
            LayersCounter.Value = 0;
            ActualTilesCounter.Value = 0;
            AllocatedTilesCounter.Value = 0;
        }

        public TileSet(BoxBoundingVolume areaOfInterest, int initialSize = 64, Allocator alloc = Allocator.Persistent)
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
            Transform = new NativeList<float4x4>(initialSize, alloc);

            // Assume 4 children per tile and have the list autogrow. This matches the concept of quad trees, and
            // even though these should be defined as implicit tilesets - it is a useful metric.
            Children = new Buffer<int>(initialSize, initialSize * 4, alloc);

            // Assume that tiles have a single content by default, there could be multiple but generally there is only 1
            Contents = new Buffer<TileContentData>(initialSize, initialSize, alloc);

            // Assume strings have a length of 128 bytes on average
            Strings = new StringTable(initialSize, initialSize * 128, alloc);
            
            // TODO: these initial capacities should be changed
            Warm = new NativeList<int>(128, alloc);
            Hot = new NativeList<int>(32, alloc);
        }

        public int AddTile(
            in BoxBoundingVolume boundingVolume,
            double geometricError,
            ReadOnlySpan<TileContentData> contents,
            ReadOnlySpan<int> children = default,
            MethodOfRefinement refine = MethodOfRefinement.Replace,
            in float4x4 transform = default
        ) {
            // Take any of the arrays whose length matches the number of tiles in this storage and use it's length
            // as the new id as this is last id + 1
            int id = NextTileIndex;

            ActualTilesCounter.Value += 1;
            
            BoundingVolumes.Add(id, boundingVolume);
            GeometricError.AddNoResize(geometricError);
            Refine.AddNoResize(refine);
            Transform.AddNoResize(transform);

            Children.Add(children);
            Contents.Add(contents);

            return id;
        }
        
        public Tile GetTile(int tileIndex)
        {
            return new Tile(this, tileIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetGeometricError(int tileIndex) => GeometricError[tileIndex];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BoundingVolume GetBoundingVolume(int tileIndex) => new(this, tileIndex);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4x4 GetTransform(int tileIndex) => Transform[tileIndex];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MethodOfRefinement GetMethodOfRefinement(int tileIndex) => Refine[tileIndex];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BufferBlock<int> GetChildren(int tileIndex) => Children[tileIndex];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TileContents GetContents(int tileIndex) => new (this, Contents.GetBlockById(tileIndex));

        public int WarmTile(int tileIndex)
        {
            // If the tile is already warm - just return the index. This makes it idempotent
            if (Warm.Contains(tileIndex))
            {
                return Warm.IndexOf(tileIndex);
            }

            var warmIdx = Warm.Length;
            Warm.AddNoResize(tileIndex);
            return warmIdx;
        }

        public int HeatTile(int tileIndex)
        {
            // If the tile is already warm - just return the index. This makes it idempotent
            if (Hot.Contains(tileIndex))
            {
                return Hot.IndexOf(tileIndex);
            }

            var hotIdx = Hot.Length;
            Hot.AddNoResize(tileIndex);
            return hotIdx;
        }

        public void Dispose()
        {
            // Clear all storages first to allow for a proper cleanup before disposing
            Clear();
            
            LayersCounter.Value -= 1;
            AllocatedTilesCounter.Value -= GeometricError.Capacity;
            
            GeometricError.Dispose();
            Refine.Dispose();
            Transform.Dispose();

            Children.Dispose();
            Contents.Dispose();
            Strings.Dispose();
        }

        public void Clear()
        {
            // Clearing will not free memory, but will reset the counters for the actual tiles - allocated tiles will stay the same
            ActualTilesCounter.Value -= NumberOfTiles;

            GeometricError.Clear();
            Refine.Clear();
            Transform.Clear();

            Children.Clear();
            Contents.Clear();
            Strings.Clear();
        }
    }
}