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
        private Plane[] frustumPlanes = new Plane[6]; 
        private readonly int capacity;

        public TileStateScheduler(TilesSelector tileSelector, ITileLifecycleBehaviour tileLifecycleBehaviour, TileSet tileSet, int capacity = 64)
        {
            this.tileSelector = tileSelector;
            this.tileLifecycleBehaviour = tileLifecycleBehaviour;
            this.tileSet = tileSet;
            this.capacity = capacity;
        }

        // Tiles are selected using a zoned approach, where we can select a subset of tiles that are in a certain zone. 
        // There is a warm and hot zone, and when tiles who are active in either zone migrate to another zone - they
        // should either warm/heat up or cool down/freeze.

        // Tiles are not only selected on their spatial property - but also on their LOD or temporal properties, where 
        // selection criteria is based on information from the global system - such as camera position and time of day
        public void Schedule()
        {
            // TODO: this can probably be done more memory efficient - let's roll with it for now
            var tilesInFrustrum = new NativeHashSet<int>(capacity, Allocator.Temp);
            var warmTileIndices = new NativeHashSet<int>(capacity, Allocator.Temp);
            var shouldWarmUp = new NativeHashSet<int>(capacity, Allocator.Temp);
            var shouldHeatUp = new NativeHashSet<int>(capacity, Allocator.Temp);
            var shouldCooldown = new NativeHashSet<int>(capacity, Allocator.Temp);
            var shouldFreeze = new NativeHashSet<int>(capacity, Allocator.Temp);

            GeometryUtility.CalculateFrustumPlanes(Camera.main, frustumPlanes);
            
            // TODO: this only matches hot tiles, we should do another pass with a larger feather for the warm area
            tileSelector.Select(tilesInFrustrum, tileSet.Root, frustumPlanes);

            // For each tile that is warm in the tileSet, check if it should cool down
            for (var warmTileIndex = 0; warmTileIndex < tileSet.Warm.Length; warmTileIndex++)
            {
                var warmTile = tileSet.Warm[warmTileIndex];
                warmTileIndices.Add(warmTile);
                
                // Tile is in frustum, so it should not cool down
                if (tilesInFrustrum.Contains(warmTile)) continue;
                
                // TODO: Split this - at the moment we assume all tiles in frustum do the same, but this should be zoned
                shouldCooldown.Add(warmTile);
                shouldFreeze.Add(warmTile);
            }

            // For each tile is in frustum, check it it should warm up
            foreach (var tileInFrustum in tilesInFrustrum)
            {
                // Tile is already warm, no need to try again
                if (warmTileIndices.Contains(tileInFrustum)) continue;

                // TODO: Split this - at the moment we assume all tiles in frustum do the same, but this should be zoned
                shouldWarmUp.Add(tileInFrustum);
                shouldHeatUp.Add(tileInFrustum);
            }

            tileLifecycleBehaviour.OnWarmUp(shouldWarmUp.ToNativeArray(Allocator.Temp));
            tileLifecycleBehaviour.OnHeatUp(shouldHeatUp.ToNativeArray(Allocator.Temp));
            tileLifecycleBehaviour.OnCooldown(shouldCooldown.ToNativeArray(Allocator.Temp));
            tileLifecycleBehaviour.OnFreeze(shouldFreeze.ToNativeArray(Allocator.Temp));

            tilesInFrustrum.Dispose();
            warmTileIndices.Dispose();
            shouldWarmUp.Dispose();
            shouldHeatUp.Dispose();
            shouldCooldown.Dispose();
            shouldFreeze.Dispose();
        }
    }
}