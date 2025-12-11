using System.Runtime.CompilerServices;
using Netherlands3D.Tilekit.MemoryManagement;
using Netherlands3D.Tilekit.WriteModel;
using Unity.Collections;
using Unity.Mathematics;

namespace Netherlands3D.Tilekit.TileSets
{
    public class RasterTileSet : TileSet
    {
        public readonly struct Tile
        {
            private readonly int tileIndex;
            private readonly RasterTileSet tileSet;

            public Tile(RasterTileSet tileSet, int tileIndex)
            {
                this.tileSet = tileSet;
                this.tileIndex = tileIndex;
            }

            public int Index => tileIndex;
            public BoundingVolume BoundingVolume => new (tileSet, tileIndex);
            public double GeometricError => tileSet.GeometricError[tileIndex];
            public MethodOfRefinement Refinement => tileSet.Refine[tileIndex];

            public float4x4 Transform => tileSet.Transform[tileIndex];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TileContents Contents() => new(tileSet, tileSet.Contents.GetBlockById(tileIndex));

            // TODO: It can be confusing to return the 'absolute' children indices instead of the relative ones - you can't reuse this
            //   in the GetChild method
            public BufferBlock<int> Children()
            {
                return tileSet.Children.GetBlockById(tileIndex);
            }

            public Tilekit.Tile GetChild(int childIndex) => tileSet.GetTile(Children()[childIndex]);
            public bool IsWarm => tileSet.Warm.Contains(tileIndex);
            private int WarmIndex => tileSet.Warm.IndexOf(tileIndex);
            public bool IsHot => tileSet.Hot.Contains(tileIndex);
            public ulong Texture2DRef => !IsWarm ? ulong.MaxValue : tileSet.TextureRef[WarmIndex];
        }

        public NativeArray<ulong> TextureRef;
        
        public RasterTileSet(BoxBoundingVolume areaOfInterest, int initialSize = 64, Allocator alloc = Allocator.Persistent) : base(areaOfInterest, initialSize, alloc)
        {
            TextureRef = new NativeArray<ulong>(Warm.Capacity, alloc);
        }
    }
}