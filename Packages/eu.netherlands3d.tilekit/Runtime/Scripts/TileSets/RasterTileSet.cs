using System.Runtime.CompilerServices;
using Netherlands3D.Tilekit.ContentLoaders;
using Netherlands3D.Tilekit.MemoryManagement;
using Netherlands3D.Tilekit.WriteModel;
using Unity.Collections;
using Unity.Mathematics;

namespace Netherlands3D.Tilekit.TileSets
{
    public class RasterTileSet : TileSet
    {
        // uint2.zero means - no ref. A cache key should never result in the value 0, so we can (ab)use this to signify: there is no texture here
        private static readonly uint2 NO_TEXTURE = uint2.zero;

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
            
            public uint2 Texture2DRef => !IsWarm ? NO_TEXTURE : tileSet.TextureRef[WarmIndex];
        }

        public NativeArray<uint2> TextureRef;
        
        public RasterTileSet(BoxBoundingVolume areaOfInterest, int capacity = 64, int warmCapacity = 0, int hotCapacity = 0, Allocator alloc = Allocator.Persistent) 
            : base(areaOfInterest, capacity, warmCapacity, hotCapacity, alloc)
        {
            TextureRef = new NativeArray<uint2>(Warm.Capacity, alloc);
        }

        public void LoadTexture(int warmIdx, string url)
        {
            TextureRef[warmIdx] = Texture2DLoader.HashUrl(url);
            Texture2DLoader.Instance.Load(url);
        }

        public bool UnloadTexture(int warmIndex)
        {
            if (!Texture2DLoader.Instance.TryEvict(TextureRef[warmIndex])) return false;
         
            // TODO: can we make this a reordering buffer class? to minimize bugs
            // TODO: Or make the buffer class not append-only and signify a free slot using a value of -1?
            // Reorder texture refs
            for (int i = warmIndex; i < Warm.Length; i++)
            {
                if (i == 0) continue;
                TextureRef[i -1] = TextureRef[i];
            }
            // Clear last ref because we are going to remove the tile from tileSet.Warm
            TextureRef[Warm.Length] = NO_TEXTURE;
            
            return true;
        }

    }
}