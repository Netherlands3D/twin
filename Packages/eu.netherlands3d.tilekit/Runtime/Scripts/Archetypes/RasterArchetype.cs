using System.Runtime.InteropServices;
using Netherlands3D.Tilekit.BoundingVolumes;
using Unity.Collections;

namespace Netherlands3D.Tilekit.Archetypes
{
    public class RasterArchetype : Archetype<RasterArchetype.WarmTile, RasterArchetype.HotTile>
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct WarmTile : IHasTileIndex
        {
            public int TileIndex { get; set; }
            public ulong TextureKey; // 0 = none
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HotTile : IHasWarmTileIndex
        {
            public int WarmTileIndex { get; set; }
        }

        public RasterArchetype(BoxBoundingVolume areaOfInterest, int initialCapacity, Allocator alloc) : base(areaOfInterest, initialCapacity, alloc)
        {
        }
    }
}