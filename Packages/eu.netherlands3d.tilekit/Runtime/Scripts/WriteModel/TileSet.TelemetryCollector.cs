using System.Runtime.CompilerServices;
using Netherlands3D.Tilekit.MemoryManagement;
using Netherlands3D.Tilekit.Profiling;

namespace Netherlands3D.Tilekit.WriteModel
{
    public partial class TileSet
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Collect(ref TileSetStats stats)
        {
            // Tiles
            stats.TilesAllocated += Capacity;
            stats.TilesActual += Count;

            // Estimated Memory
            stats.NativeReservedBytes += GetReservedBytes();
            stats.NativeUsedBytes += GetUsedBytes();

            // Warm/Hot
            stats.WarmCount += Warm.IsCreated ? Warm.Length : 0;
            stats.HotCount += Hot.IsCreated ? Hot.Length : 0;

            // StringBuffers
            stats.StringsActual += Strings.Length;
            stats.StringsAllocated += Strings.Capacity;

            // StringBuffers
            stats.UrisActual += ContentUrls.Count;
            stats.UrisAllocated += ContentUrls.Capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long GetReservedBytes()
        {
            return GeometricError.GetReservedBytes()
               + Refine.GetReservedBytes()
               + Transform.GetReservedBytes()
               + Children.GetReservedBytes()
               + Contents.GetReservedBytes()
               + Strings.GetReservedBytes()
               + BoundingVolumes.GetReservedBytes()
               + ContentUrls.GetReservedBytes()
               + Warm.GetReservedBytes()
               + Hot.GetReservedBytes()
            ;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long GetUsedBytes()
        {
            return GeometricError.GetReservedBytes()
               + Refine.GetUsedBytes()
               + Transform.GetUsedBytes()
               + Children.GetUsedBytes()
               + Contents.GetUsedBytes()
               + Strings.GetUsedBytes()
               + BoundingVolumes.GetUsedBytes()
               + ContentUrls.GetUsedBytes()
               + Warm.GetUsedBytes()
               + Hot.GetUsedBytes()
            ;
        }
    }
}