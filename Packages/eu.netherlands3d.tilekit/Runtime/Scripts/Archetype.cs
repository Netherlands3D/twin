using System;
using System.Runtime.InteropServices;
using Netherlands3D.Tilekit.WriteModel;
using Unity.Collections;

namespace Netherlands3D.Tilekit
{
    public interface IHasTileIndex
    {
        int TileIndex { get; }
    }

    public interface IHasWarmTileIndex
    {
        int WarmTileIndex { get; }
    }
    
    public class Archetype<TWarmTile, THotTile> : IDisposable 
        where TWarmTile : unmanaged, IHasTileIndex 
        where THotTile : unmanaged, IHasWarmTileIndex
    {
        private readonly TileSet cold;
        public NativeList<TWarmTile> warm;
        public NativeList<THotTile> hot;
        
        public TileSet Cold => cold;
        public NativeList<TWarmTile> Warm => warm;
        public NativeList<THotTile> Hot => hot;
        
        public Archetype(BoxBoundingVolume areaOfInterest, int initialCapacity = 1024, Allocator alloc = Allocator.Persistent)
        {
            cold = new TileSet(areaOfInterest, initialCapacity, alloc);
            warm = new NativeList<TWarmTile>(128, alloc);
            hot = new NativeList<THotTile>(64, alloc);
        }
        
        public void Clear()
        {
            cold.Clear();
            warm.Clear();
            hot.Clear();
        }

        public void Dispose()
        {
            cold.Dispose();
            warm.Dispose();
            hot.Dispose();
        }
    }
}