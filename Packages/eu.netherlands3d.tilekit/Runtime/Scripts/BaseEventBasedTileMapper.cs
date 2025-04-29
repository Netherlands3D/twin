using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Tilekit;
using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Tilekit.TileSets;
using Netherlands3D.Twin.Tilekit.Events;
using RSG;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Tilekit.TileMappers
{
    public abstract class BaseEventBasedTileMapper : BaseTileMapper
    {
        [field:SerializeField] public string Id { get; private set; } = Guid.NewGuid().ToString();
        
        [Header("Events")]
        public UnityEvent<TileSet> Loaded = new();
        
        public EventChannel EventChannel => EventBus.Channel(Id);
        public EventSource EventSource { get; private set; }

        protected virtual IEnumerator Start()
        {
            EventChannel.TileSetLoaded += OnTileSetLoaded;
            EventChannel.FrustumChanged += OnFrustumChanged;
            EventChannel.TilesSelected += OnTilesSelected;
            EventChannel.TransitionCreated += OnTransition;
            EventChannel.ChangeScheduleRequested += OnChangeScheduleRequested;
            EventChannel.ChangesScheduled += OnChangesPlanned;
            EventChannel.ChangeApply += OnChangeApply;
            
            yield break;
        }

        private void OnDestroy()
        {
            EventChannel.TileSetLoaded -= OnTileSetLoaded;
            EventChannel.FrustumChanged -= OnFrustumChanged;
            EventChannel.TilesSelected -= OnTilesSelected;
            EventChannel.TransitionCreated -= OnTransition;
            EventChannel.ChangeScheduleRequested -= OnChangeScheduleRequested;
            EventChannel.ChangesScheduled -= OnChangesPlanned;
            EventChannel.ChangeApply -= OnChangeApply;
        }

        public override void Load(TileSet tileSet)
        {
            EventSource = new EventSource(EventChannel.Id, tileSet);

            base.Load(tileSet);
            
            EventChannel.RaiseTileSetLoaded(EventSource, tileSet);
        }

        public void TriggerUpdate()
        {
            EventChannel.RaiseTriggerUpdated(EventSource);
        }

        private void OnTileSetLoaded(EventSource source, TileSet tileSet)
        {
            Loaded.Invoke(tileSet);
        }

        protected virtual void OnFrustumChanged(EventSource eventSource, Plane[] planes)
        {
        }

        protected virtual void OnTilesSelected(EventSource eventSource, Tiles tiles)
        {
        }

        protected virtual void OnTransition(EventSource eventSource, List<Change> transition)
        {
        }

        protected virtual void OnChangeScheduleRequested(EventSource eventSource, Change change)
        {
        }

        protected virtual void OnChangesPlanned(EventSource eventSource, List<Change> changes)
        {
        }

        protected virtual Promise OnChangeApply(EventSource eventSource, Change change)
        {
            return Promise.Resolved() as Promise;
        }
    }
}