using System.Runtime.CompilerServices;
using Netherlands3D.Tilekit.MemoryManagement;
using Netherlands3D.Tilekit.WriteModel;
using Unity.Mathematics;

namespace Netherlands3D.Tilekit
{
    public readonly struct Tile
    {
        private readonly int tileIndex;
        private readonly TileSet tileSet;

        public Tile(TileSet tileSet, int tileIndex)
        {
            this.tileSet = tileSet;
            this.tileIndex = tileIndex;
        }

        public int Index => tileIndex;
        public BoundingVolume BoundingVolume => tileSet.GetBoundingVolume(tileIndex);
        public double GeometricError => tileSet.GetGeometricError(tileIndex);
        public MethodOfRefinement Refinement => tileSet.GetMethodOfRefinement(tileIndex);

        public float4x4 Transform => tileSet.GetTransform(tileIndex);

        public TileContents Contents => tileSet.GetContents(tileIndex);

        // TODO: It can be confusing to return the 'absolute' children indices instead of the relative ones - you can't reuse this
        //   in the GetChild method
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BufferBlock<int> Children() => tileSet.GetChildren(tileIndex);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Tile GetChild(int childIndex) => tileSet.GetTile(Children()[childIndex]);
    }
}