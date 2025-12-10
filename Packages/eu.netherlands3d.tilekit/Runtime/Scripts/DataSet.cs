using System;
using System.Collections;
using Netherlands3D.Coordinates;
using Netherlands3D.Tilekit.ExtensionMethods;
using Netherlands3D.Tilekit.WriteModel;
using Unity.Collections;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    [RequireComponent(typeof(Timer))]
    public abstract class DataSet<TTileSet> : MonoBehaviour, ITileLifecycleBehaviour where TTileSet : TileSet 
    {
        public bool IsInitialized { get; set; } = false;

        protected Timer timer;

        protected TTileSet tileSet;
        private TileStateScheduler stateScheduler;

        private void Awake()
        {
            timer = GetComponent<Timer>();
        }

        protected abstract TTileSet CreateTileSet();

        private IEnumerator Start()
        {
            // Wait two frames for the switch to main scene
            yield return null;
            yield return null;

            tileSet = CreateTileSet();
            stateScheduler = new (new TilesSelector(), this, tileSet);

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

        public abstract void OnWarmUp(ReadOnlySpan<int> candidateTileIndices);
        public abstract void OnHeatUp(ReadOnlySpan<int> candidateTileIndices);
        public abstract void OnCooldown(ReadOnlySpan<int> candidateTileIndices);
        public abstract void OnFreeze(ReadOnlySpan<int> candidateTileIndices);

        protected virtual void OnTick()
        {
            stateScheduler.Schedule();
        }

        protected virtual void OnDestroy()
        {
            tileSet?.Dispose();
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
                return;

            if (!tileSet.Warm.IsCreated || !tileSet.Hot.IsCreated)
                return;

            DrawTileGizmo(tileSet.Root);
        }

        private void DrawTileGizmo(Tile tile, int height = 0)
        {
            var bounds = tile.BoundingVolume.ToBounds().ToLocalCoordinateSystem(CoordinateSystem.RD);
            
            Gizmos.color = Color.grey;
            if (tileSet.Warm.Contains(tile.Index)) Gizmos.color = Color.yellow;
            if (tileSet.Hot.Contains(tile.Index)) Gizmos.color = Color.red;

            if (Gizmos.color != Color.grey)
            {
                // Gizmos.DrawWireCube(bounds.center + Vector3.up * 0.1f, bounds.size);
            }
            if (Gizmos.color == Color.red)
            {
                Gizmos.DrawWireCube(bounds.center + Vector3.up * 0.1f, bounds.size);
            }

            for (int i = 0; i < tile.Children().Length; i++)
            {
                DrawTileGizmo(tile.GetChild(i), height + 1);
            }
        }
    }
}