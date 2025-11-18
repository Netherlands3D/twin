using System.Runtime.InteropServices;
using Unity.Collections;

namespace Netherlands3D.Tilekit.Archetypes
{
    public class RasterArchetype : Archetype<RasterArchetype.WarmTile, RasterArchetype.HotTile>
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct WarmTile
        {
            public int TileIndex;
            public ulong TextureKey; // 0 = none
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HotTile
        {
            public int WarmTileIndex;
        }

        public RasterArchetype(int initialCapacity, Allocator alloc) : base(initialCapacity, alloc)
        {
        }
    }
}