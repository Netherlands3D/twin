using Netherlands3D.Tilekit.WriteModel;
using Unity.Collections;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    public class TileStateScheduler
    {
        private TilesSelector tileSelector;
        private ITileLifecycleBehaviour tileLifecycleBehaviour;
        private TileSet tileSet;

        public TileStateScheduler(TilesSelector tileSelector, ITileLifecycleBehaviour tileLifecycleBehaviour, TileSet tileSet)
        {
            this.tileSelector = tileSelector;
            this.tileLifecycleBehaviour = tileLifecycleBehaviour;
            this.tileSet = tileSet;
        }

        public void Schedule()
        {
            var tilesInFrustrum = new NativeHashSet<int>(1024, Allocator.Temp);
            var warmTileIndices = new NativeHashSet<int>(1024, Allocator.Temp);
            var shouldWarmUp = new NativeHashSet<int>(1024, Allocator.Temp);
            var shouldFreeze = new NativeHashSet<int>(1024, Allocator.Temp);

            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            tileSelector.Select(tilesInFrustrum, tileSet.Root, frustumPlanes);

            // Tiles are selected using a zoned approach, where we can select a subset of tiles that are in a certain zone. 
            // There is a warm and hot zone, and when tiles who are active in either zone migrate to another zone - they
            // should either warm/heat up or cool down/freeze.

            // Tiles are not only selected on their spatial property - but also on their LOD or temporal properties, where 
            // selection criteria is based on information from the global system - such as camera position and time of day

            for (var warmTileIndex = 0; warmTileIndex < tileSet.Warm.Length; warmTileIndex++)
            {
                var warmTile = tileSet.Warm[warmTileIndex];
                warmTileIndices.Add(warmTile);
                if (tilesInFrustrum.Contains(warmTile)) continue;
                
                shouldFreeze.Add(warmTile);
            }

            foreach (var tileInFrustum in tilesInFrustrum)
            {
                if (warmTileIndices.Contains(tileInFrustum)) continue;
                
                shouldWarmUp.Add(tileInFrustum);
            }

            tileLifecycleBehaviour.OnWarmUp(shouldWarmUp.ToNativeArray(Allocator.Temp));
            // TODO: Heat up
            // TODO: Cool down
            tileLifecycleBehaviour.OnFreeze(shouldFreeze.ToNativeArray(Allocator.Temp));

            tilesInFrustrum.Dispose();
            warmTileIndices.Dispose();
            shouldWarmUp.Dispose();
            shouldFreeze.Dispose();
        }
    }
}