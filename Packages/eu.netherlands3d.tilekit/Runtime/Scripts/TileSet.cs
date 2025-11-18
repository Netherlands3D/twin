using System;
using Netherlands3D.Tilekit.BoundingVolumes;
using Netherlands3D.Tilekit.WriteModel;
using Unity.Collections;
using Unity.Mathematics;

namespace Netherlands3D.Tilekit
{
    public sealed class TileSet : IDisposable
    {
        private ColdStorage tiles;
        public Tile Root { get; }

        public TileSet(int initialSize = 64, Allocator alloc = Allocator.Persistent)
        {
            tiles = new ColdStorage(initialSize, alloc);
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