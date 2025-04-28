using System;
using Netherlands3D.Tilekit.TileSets.ImplicitTiling;
using Unity.Collections;
using UnityEngine;

namespace Netherlands3D.Tilekit.TileSets
{
    public struct Tile
    {
        public NativeText Id;
        public BoundingVolume BoundingVolume;
        public double GeometricError;
        public NativeList<TileContent> TileContents;
        public Matrix4x4 Transform;
        public NativeList<Tile> Children;
        public ImplicitTilingScheme ImplicitTiling;
        public MethodOfRefinement Refine;

        public Tile(BoundingVolume boundingVolume, double geometricError)
        {
            Id = new NativeText(Guid.NewGuid().ToString(), Allocator.Persistent);
            BoundingVolume = boundingVolume;
            GeometricError = geometricError;
            TileContents = new();
            Children = new();
            ImplicitTiling = new ImplicitTilingScheme(new None());
            Refine = MethodOfRefinement.Replace;
            Transform = Matrix4x4.identity;
        }
    }
}
