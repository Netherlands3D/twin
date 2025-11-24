using System;
using System.Collections;
using Netherlands3D.Coordinates;
using Netherlands3D.Tilekit.ExtensionMethods;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    [RequireComponent(typeof(Timer))]
    public abstract class ServiceType<TArchetype, TWarmTile, THotTile> : MonoBehaviour, ITileLifecycleBehaviour 
        where TArchetype : Archetype<TWarmTile, THotTile>
        where TWarmTile : unmanaged, IHasTileIndex 
        where THotTile : unmanaged, IHasWarmTileIndex
    {
        public bool IsInitialized { get; set; } = false;

        protected Timer timer;
        protected TArchetype archetype;
        private TileStateScheduler<TArchetype, TWarmTile, THotTile> stateScheduler;

        private void Awake()
        {
            timer = GetComponent<Timer>();
        }

        private IEnumerator Start()
        {
            // Wait two frames for the switch to main scene
            yield return null;
            yield return null;

            stateScheduler = new (new TilesSelector(), this, archetype);

            timer.tick.AddListener(OnTick);
            Initialize();
            IsInitialized = true;
            timer.Resume();
        }
        
        private void OnEnable()
        {
            if (IsInitialized) timer.Resume();
        }

        private void OnDisable()
        {
            timer.Pause();
        }
        
        protected virtual void Initialize()
        {
            
        }

        protected virtual void OnTick()
        {
            stateScheduler.Schedule();
        }

        protected virtual void OnDestroy()
        {
            archetype?.Dispose();
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
                return;

            if (!archetype.Warm.IsCreated || !archetype.Hot.IsCreated)
                return;

            var tile = archetype.Cold.Get(0);
            DrawTileGizmo(tile);
        }

        private void DrawTileGizmo(Tile tile, int height = 0)
        {
            var bounds = tile.BoundingVolume.ToBounds().ToLocalCoordinateSystem(CoordinateSystem.RD);
            
            Gizmos.color = Color.white;
            // TODO: can we make this O(1)?
            for (var index = 0; index < archetype.Warm.Length; index++)
            {
                if (archetype.Warm[index].TileIndex == tile.Index) Gizmos.color = Color.yellow;
            }
            // TODO: can we make this O(1)?
            for (var index = 0; index < archetype.Hot.Length; index++)
            {
                if (archetype.Warm[archetype.Hot[index].WarmTileIndex].TileIndex == tile.Index) Gizmos.color = Color.red;
            }

            if (Gizmos.color != Color.white)
                Gizmos.DrawWireCube(bounds.center, bounds.size);

            for (int i = 0; i < tile.Children().Count; i++)
            {
                DrawTileGizmo(tile.GetChild(i), height + 1);
            }
        }

        public abstract void OnWarmUp(ReadOnlySpan<int> candidateTileIndices);
        public abstract void OnHeatUp(ReadOnlySpan<int> candidateTileIndices);
        public abstract void OnCooldown(ReadOnlySpan<int> candidateTileIndices);
        public abstract void OnFreeze(ReadOnlySpan<int> candidateTileIndices);
    }
}