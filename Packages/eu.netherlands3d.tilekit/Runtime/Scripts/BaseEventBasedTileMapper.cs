using System;
using System.Collections.Generic;
using Netherlands3D.Tilekit;
using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Tilekit.TileSets;
using Netherlands3D.Twin.Tilekit.Events;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Tilekit.TileMappers
{
    public abstract class BaseEventBasedTileMapper : BaseTileMapper
    {
        [Header("Events")]
        public UnityEvent<TileSet> Loaded = new();
        
        public TileSetEventStream EventStream { get; private set; }
        public TileSetEventStreamContext EventStreamContext { get; private set; }

        private void Awake()
        {
            // Cache a reference to the EventChannel for later use
            EventStream = EventBus.Stream(TileSetProvider.TileSetId);
        }

        protected virtual void Start()
        {
            EventStream.TileSetLoaded.AddListener(OnTileSetLoaded);
            EventStream.FrustumChanged.AddListener(OnFrustumChanged);
            EventStream.TilesSelected.AddListener(OnTilesSelected);
            EventStream.TransitionCreated.AddListener(OnTransition);
            EventStream.ChangeScheduleRequested.AddListener(OnChangeScheduleRequested);
            EventStream.ChangesScheduled.AddListener(OnChangesPlanned);
            EventStream.ChangeApply.AddListener(OnChangeApply);
        }

        private void OnDestroy()
        {
            EventStream.TileSetLoaded.RemoveListener(OnTileSetLoaded);
            EventStream.FrustumChanged.RemoveListener(OnFrustumChanged);
            EventStream.TilesSelected.RemoveListener(OnTilesSelected);
            EventStream.TransitionCreated.RemoveListener(OnTransition);
            EventStream.ChangeScheduleRequested.RemoveListener(OnChangeScheduleRequested);
            EventStream.ChangesScheduled.RemoveListener(OnChangesPlanned);
            EventStream.ChangeApply.RemoveListener(OnChangeApply);
        }

        public void TriggerUpdate()
        {
            EventStream.UpdateTriggered.Invoke(EventStreamContext);
        }

        protected virtual void OnTileSetLoaded(TileSetEventStreamContext streamContext, TileSet tileSet)
        {
            // Cache an EventSource instance for convenience
            EventStreamContext = new TileSetEventStreamContext(TileSetProvider.TileSetId, tileSet);

            Loaded.Invoke(tileSet);
        }

        protected virtual void OnFrustumChanged(TileSetEventStreamContext eventStreamContext, Plane[] planes)
        {
        }

        protected virtual void OnTilesSelected(TileSetEventStreamContext eventStreamContext, Tiles tiles)
        {
        }

        protected virtual void OnTransition(TileSetEventStreamContext eventStreamContext, List<Change> transition)
        {
        }

        protected virtual void OnChangeScheduleRequested(TileSetEventStreamContext eventStreamContext, Change change)
        {
        }

        protected virtual void OnChangesPlanned(TileSetEventStreamContext eventStreamContext, List<Change> changes)
        {
        }

        protected virtual void OnChangeApply(TileSetEventStreamContext eventStreamContext, Change change)
        {
            
        }
    }
}