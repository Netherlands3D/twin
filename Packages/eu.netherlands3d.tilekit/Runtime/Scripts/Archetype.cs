using System;
using Netherlands3D.Tilekit.WriteModel;
using Unity.Collections;

namespace Netherlands3D.Tilekit
{
    public class Archetype<TWarmTile, THotTile> : IDisposable where TWarmTile : unmanaged where THotTile : unmanaged
    {
        private readonly ColdStorage cold;
        public NativeList<TWarmTile> warm;
        public NativeList<THotTile> hot;
        
        public ColdStorage Cold => cold;
        public NativeList<TWarmTile> Warm => warm;
        public NativeList<THotTile> Hot => hot;
        
        public Archetype(int initialCapacity, Allocator alloc)
        {
            cold = new ColdStorage(initialCapacity, alloc);
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