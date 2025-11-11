using System;
using System.Runtime.CompilerServices;
using Netherlands3D.Tilekit.Optimized.TileSets;
using Unity.Collections;
using Unity.Mathematics;
using MethodOfRefinement = Netherlands3D.Tilekit.Optimized.TileSets.MethodOfRefinement;
using SubdivisionScheme = Netherlands3D.Tilekit.Optimized.TileSets.SubdivisionScheme;
using Tile = Netherlands3D.Tilekit.Optimized.TileSets.Tile;

namespace Netherlands3D.Tilekit.Optimized
{
    public sealed class TileSet : IDisposable
    {
        private TilesStorage tiles;
        public Tile Root { get; }

        public TileSet(int initialSize = 64, Allocator alloc = Allocator.Persistent)
        {
            tiles = new TilesStorage(initialSize, alloc);
            Root = new Tile(tiles, 0);
        }

        public int AddTile(
            in BoxBoundingVolume boundingVolume,
            double geometricError,
            MethodOfRefinement refine,
            SubdivisionScheme subdivision,
            in float4x4 transform,
            ReadOnlySpan<int> children,
            ReadOnlySpan<TileContentData> contents)
        {
            return tiles.AddTile(
                boundingVolume,
                geometricError,
                contents,
                children,
                refine, 
                subdivision,
                transform
            );
        }

        public void Dispose()
        {
            tiles.Dispose();
        }
    }
}

namespace Netherlands3D.Tilekit.Optimized.TileSets
{
    /// A typed, allocation-free view over a single content entry.
    public readonly struct TileContent
    {
        private readonly TilesStorage store;
        private readonly TileContentData data;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TileContent(TilesStorage store, in TileContentData data)
        {
            this.store = store;
            this.data = data;
        }

        public BoundingVolumeRef Bounds => data.BoundingVolume;
        private int UriIndex => data.UriIndex;

        /// Returns false if UriIndex < 0 or string truncated to 127 chars.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetUri(out FixedString128Bytes uri)
        {
            if (UriIndex < 0)
            {
                uri = default;
                return false;
            }

            return store.Strings.TryGetFixedString128(UriIndex, out uri);
        }
    }

    /// A typed, allocation-free view over all contents of a tile.
    /// NOTE: This wraps a NativeSlice under the hood; if the underlying NativeList grows,
    /// previously captured views become invalid. Use immediately or seal storage to NativeArray.
    public struct TileContents
    {
        private readonly TilesStorage store;
        private Bucket<TileContentData> bucket;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TileContents(TilesStorage store, Bucket<TileContentData> bucket)
        {
            this.store = store;
            this.bucket = bucket;
        }

        public int Count => bucket.Count;

        public TileContent this[int i] => new(store, bucket[i]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<TileContentData>.Enumerator GetEnumerator() => bucket.GetEnumerator();
    }

    /// <summary>
    /// Union type for all bounding volume types.
    /// </summary>
    public readonly struct BoundingVolume
    {
        private readonly TilesStorage store;
        private readonly int index;
        
        public BoundingVolume(TilesStorage store, int index)
        {
            this.store = store;
            this.index = index;
        }
        
        public BoxBoundingVolume AsBox() => store.BoundingVolumes.Boxes[index];
        public RegionBoundingVolume AsRegion() => store.BoundingVolumes.Regions[index];
        public SphereBoundingVolume AsSphere() => store.BoundingVolumes.Spheres[index];

        public BoundsDouble ToBounds()
        {
            var boundingVolumeRef = store.BoundingVolumes.BoundingVolumeRefs[index];
            return boundingVolumeRef.Type switch
            {
                BoundingVolumeType.Region => AsRegion().ToBounds(),
                BoundingVolumeType.Box => AsBox().ToBounds(),
                BoundingVolumeType.Sphere => AsSphere().ToBounds(),
                _ => throw new Exception("Invalid bounding volume type")
            };
        }
    }
    
    public readonly struct Tile
    {
        private readonly int tileIndex;
        private readonly TilesStorage store;

        public Tile(TilesStorage store, int tileIndex)
        {
            this.store = store;
            this.tileIndex = tileIndex;
        }

        public int Index => tileIndex;
        public BoundingVolume BoundingVolume => new (store, tileIndex);
        public double GeometricError => store.GeometricError[tileIndex];
        public MethodOfRefinement Refinement => store.Refine[tileIndex];
        public SubdivisionScheme Subdivision => store.Subdivision[tileIndex];

        public float4x4 Transform => store.Transform[tileIndex];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TileContents Contents() => new(store, store.Contents.GetBucket(tileIndex));

        public Bucket<int> Children() => store.Children.GetBucket(tileIndex);
        public Tile GetChild(int childIndex) => store.Get(Children()[childIndex]);
        //
        // public bool TryGetName(out FixedString128Bytes name)
        // {
        //     int nameIdx = store.NameIndex[tileIndex];
        //     if (nameIdx < 0)
        //     {
        //         name = default;
        //         return false;
        //     }
        //
        //     return store.Strings.TryGetFixedString128(nameIdx, out name);
        // }
    }
}