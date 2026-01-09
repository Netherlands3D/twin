using System;
using Netherlands3D.Tilekit.Profiling;
using Netherlands3D.Tilekit.WriteModel;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    [RequireComponent(typeof(Timer))]
    public abstract class DataSet : MonoBehaviour, ITileLifecycleBehaviour
    {
        public bool IsInitialized { get; private set; }
        private TileSetStatsAdapter telemetry;
        private TileStateScheduler stateScheduler;

        public TileSet TileSet { get; private set; } = null!;

        protected abstract TileSet CreateTileSetBase();

        public void Initialize()
        {
            TileSet = CreateTileSetBase();
            telemetry = new TileSetStatsAdapter(GetInstanceID(), gameObject.name, TileSet);
            Telemetry.Register(telemetry);
            stateScheduler = new (new TilesSelector(), this, TileSet);
            OnInitialize();
            IsInitialized = true;
        }

        protected virtual void OnInitialize() { }

        public virtual void TickedUpdate()
        {
            stateScheduler.Schedule();
        }

        public abstract void OnWarmUp(ReadOnlySpan<int> candidateTileIndices);
        public abstract void OnHeatUp(ReadOnlySpan<int> candidateTileIndices);
        public abstract void OnCooldown(ReadOnlySpan<int> candidateTileIndices);
        public abstract void OnFreeze(ReadOnlySpan<int> candidateTileIndices);

        public void Dispose()
        {
            if (telemetry != null) Telemetry.Unregister(telemetry);
            TileSet?.Dispose();
        }
    }

    public abstract class DataSet<TTileSet> : DataSet where TTileSet : TileSet
    {
        protected new TTileSet TileSet => (TTileSet)base.TileSet;

        // Factory method to create the tileset instance for this dataset.
        protected abstract TTileSet CreateTileSet();

        // Adapter hook to avoid casting in base class
        protected sealed override TileSet CreateTileSetBase() => CreateTileSet();
    }
}