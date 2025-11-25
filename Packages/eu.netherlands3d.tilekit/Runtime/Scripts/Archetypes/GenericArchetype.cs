using System.Runtime.InteropServices;
using Netherlands3D.Tilekit.BoundingVolumes;
using Unity.Collections;

namespace Netherlands3D.Tilekit.Archetypes
{
    public class GenericArchetype : Archetype<GenericArchetype.WarmTile, GenericArchetype.HotTile>
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct WarmTile : IHasTileIndex
        {
            public int TileIndex { get; set; }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HotTile : IHasWarmTileIndex
        {
            public int WarmTileIndex { get; set; }
        }

        public GenericArchetype(BoxBoundingVolume areaOfInterest, int initialCapacity = 1024, Allocator alloc = Allocator.Persistent) : base(areaOfInterest, initialCapacity, alloc)
        {
        }
    }
}