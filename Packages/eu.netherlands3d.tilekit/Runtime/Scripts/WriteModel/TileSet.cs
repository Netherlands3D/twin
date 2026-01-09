using System;
using System.Runtime.CompilerServices;
using Netherlands3D.Tilekit.MemoryManagement;
using Unity.Collections;
using Unity.Mathematics;

namespace Netherlands3D.Tilekit.WriteModel
{
    /// <summary>
    /// The central storage location for memory related to a tileset
    /// </summary>
    public partial class TileSet : IDisposable, IMemoryReporter
    {
        private const int AVERAGE_CONTENT_ITEMS_PER_TILE = 1;
        private const int AVERAGE_STRING_LENGTH = 128;
        private const int AVERAGE_CHILDREN_PER_TILE = 4;

        public Tile Root => GetTile(0);
        private int NextTileIndex => GeometricError.Length;
        private int Count => GeometricError.Length;
        private int Capacity => GeometricError.Capacity;

        public BoxBoundingVolume AreaOfInterest;
        
        public BoundingVolumeStore BoundingVolumes;
        public NativeList<double> GeometricError; // hot
        public NativeList<MethodOfRefinement> Refine; // hot/small
        public NativeList<float4x4> Transform; // consider sparsifying if many are identity

        public readonly Buffer<int> Children;
        public readonly Buffer<TileContentData> Contents;
        public readonly StringBuffer Strings;
        public readonly UriBuffer ContentUrls;

        public NativeList<int> Warm;
        public NativeList<int> Hot;

        // Allocation and growth are aligned to 64 tiles. This ensures memory alignment 
        // and reduces fragmentation when resizing or replacing storages. Because each 
        // storage grows in fixed-size increments, freed blocks have predictable sizes 
        // and can be efficiently reused by the allocator.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsMultipleOf64(int value) => (value & 63) == 0;

        public TileSet(BoxBoundingVolume areaOfInterest, int capacity = 64, int warmCapacity = 0, int hotCapacity = 0, Allocator alloc = Allocator.Persistent)
        {
            if (!IsMultipleOf64(capacity))
            {
                throw new ArgumentException("Initial size must be a multiple of 64", nameof(capacity));
            }

            if (warmCapacity == 0)
            {
                // When no explicit warm capacity is given, assume 12.5% (or 1/8th) of the total capacity can become warm, with a minimum of 16 tiles.
                warmCapacity = Math.Max(16, (int)(capacity * .125f));
            }
            if (hotCapacity == 0)
            {
                // When no explicit warm capacity is given, assume 3.125% or (1/32th) of the total capacity can become hot, with a minimum of 8 tiles.
                hotCapacity = Math.Max(8, (int)(capacity * .03125f));
            }
            AreaOfInterest = areaOfInterest;
            
            BoundingVolumes = new BoundingVolumeStore(capacity, alloc);
            GeometricError = new NativeList<double>(capacity, alloc);
            Refine = new NativeList<MethodOfRefinement>(capacity, alloc);
            Transform = new NativeList<float4x4>(capacity, alloc);

            // Assume 4 children per tile and have the list autogrow. This matches the concept of quad trees, and
            // even though these should be defined as implicit tilesets - it is a useful metric.
            Children = new Buffer<int>(capacity, capacity * AVERAGE_CHILDREN_PER_TILE, alloc);

            // Each content item has a url composed of multiple parts - so it can exceed average content items per tile (would be weird though), let's test
            // with 10x the average length
            // TODO: Investigate if this is correct - I think so, because each content uri has a unique component, and then we have entries for the common stuff
            //    It is the byte size that should fit neatly, which I think it does?
            Strings = new StringBuffer(capacity * AVERAGE_CONTENT_ITEMS_PER_TILE * 10, capacity * AVERAGE_STRING_LENGTH * 10, alloc);

            // Assume that tiles have a single content by default, there could be multiple but generally there is only 1
            Contents = new Buffer<TileContentData>(capacity, capacity * AVERAGE_CONTENT_ITEMS_PER_TILE, alloc);
            ContentUrls = new UriBuffer(Strings, capacity * AVERAGE_CONTENT_ITEMS_PER_TILE, alloc);
            
            Warm = new NativeList<int>(warmCapacity, alloc);
            Hot = new NativeList<int>(hotCapacity, alloc);
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

        public void CoolTile(int tileIndex)
        {
            // Note: this will reorder the list - any reference tables that use the index of the Hot array should reorder
            var indexOf = Hot.IndexOf(tileIndex);
            if (indexOf == -1) return;

            Hot.RemoveAt(indexOf);
        }

        public void FreezeTile(int tileIndex)
        {
            // Note: this will reorder the list - any reference tables that use the index of the Warm array should reorder
            var indexOf = Warm.IndexOf(tileIndex);
            if (indexOf == -1) return;

            Warm.RemoveAt(indexOf);
        }

        public void Dispose()
        {
            // Clear all storages first to allow for a proper cleanup before disposing
            Clear();
            
            GeometricError.Dispose();
            Refine.Dispose();
            Transform.Dispose();

            Children.Dispose();
            Contents.Dispose();
            Strings.Dispose();
            ContentUrls.Dispose();
            BoundingVolumes.Dispose();
            Warm.Dispose();
            Hot.Dispose();
        }

        public void Clear()
        {
            GeometricError.Clear();
            Refine.Clear();
            Transform.Clear();

            Children.Clear();
            Contents.Clear();
            Strings.Clear();
            ContentUrls.Clear();
            Warm.Clear();
            Hot.Clear();
        }
    }
}