using System.Runtime.CompilerServices;
using Netherlands3D.Tilekit.MemoryManagement;
using Netherlands3D.Tilekit.WriteModel;
using Unity.Mathematics;

namespace Netherlands3D.Tilekit
{
    public readonly struct Tile
    {
        private readonly int tileIndex;
        private readonly ColdStorage store;

        public Tile(ColdStorage store, int tileIndex)
        {
            this.store = store;
            this.tileIndex = tileIndex;
        }

        public int Index => tileIndex;
        public BoundingVolume BoundingVolume => new (store, tileIndex);
        public double GeometricError => store.GeometricError[tileIndex];
        public MethodOfRefinement Refinement => store.Refine[tileIndex];

        public float4x4 Transform => store.Transform[tileIndex];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TileContents Contents() => new(store, store.Contents.GetBucket(tileIndex));

        // TODO: It can be confusing to return the 'absolute' children indices instead of the relative ones - you can't reuse this
        //   in the GetChild method
        public Bucket<int> Children()
        {
            return store.Children.GetBucket(tileIndex);
        }

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