using Netherlands3D.Tilekit.ServiceTypes;
using Unity.Collections;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    public class TileStateScheduler<TArchetype, TWarmTile, THotTile>
        where TArchetype : Archetype<TWarmTile, THotTile>
        where TWarmTile : unmanaged, IHasTileIndex
        where THotTile : unmanaged, IHasWarmTileIndex
    {
        private TilesSelector tileSelector;
        private ITileLifecycleBehaviour tileLifecycleBehaviour;
        private TArchetype archetype;

        public TileStateScheduler(TilesSelector tileSelector, ITileLifecycleBehaviour tileLifecycleBehaviour, TArchetype archetype)
        {
            this.tileSelector = tileSelector;
            this.tileLifecycleBehaviour = tileLifecycleBehaviour;
            this.archetype = archetype;
        }

        public void Schedule()
        {
            // TODO: make these fields
            var tilesInFrustrum = new NativeHashSet<int>(1024, Allocator.Temp);
            var warmTileIndices = new NativeHashSet<int>(1024, Allocator.Temp);
            var shouldWarmUp = new NativeHashSet<int>(1024, Allocator.Temp);
            var shouldFreeze = new NativeHashSet<int>(1024, Allocator.Temp);


            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            tileSelector.Select(tilesInFrustrum, archetype.Cold.Root, frustumPlanes);

            // Tiles are selected using a zoned approach, where we can select a subset of tiles that are in a certain zone. 
            // There is a warm and hot zone, and when tiles who are active in either zone migrate to another zone - they
            // should either warm/heat up or cool down/freeze.

            // Tiles are not only selected on their spatial property - but also on their LOD or temporal properties, where 
            // selection criteria is based on information from the global system - such as camera position and time of day

            foreach (var warmTile in archetype.Warm)
            {
                warmTileIndices.Add(warmTile.TileIndex);
                if (tilesInFrustrum.Contains(warmTile.TileIndex) == false)
                {
                    shouldFreeze.Add(warmTile.TileIndex);
                }
            }

            foreach (var tileInFrustum in tilesInFrustrum)
            {
                if (warmTileIndices.Contains(tileInFrustum) == false)
                {
                    shouldWarmUp.Add(tileInFrustum);
                }
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