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
        
        public TilekitEventChannel EventChannel => EventBus.Channel(Id);
        public TilekitEventSource TilekitEventSource { get; private set; }

        protected virtual void Start()
        {
            EventChannel.TileSetLoaded.AddListener(OnTileSetLoaded);
            EventChannel.FrustumChanged.AddListener(OnFrustumChanged);
            EventChannel.TilesSelected.AddListener(OnTilesSelected);
            EventChannel.TransitionCreated.AddListener(OnTransition);
            EventChannel.ChangeScheduleRequested.AddListener(OnChangeScheduleRequested);
            EventChannel.ChangesScheduled.AddListener(OnChangesPlanned);
            EventChannel.ChangeApply.AddListener(OnChangeApply);
        }

        private void OnDestroy()
        {
            EventChannel.TileSetLoaded.RemoveListener(OnTileSetLoaded);
            EventChannel.FrustumChanged.RemoveListener(OnFrustumChanged);
            EventChannel.TilesSelected.RemoveListener(OnTilesSelected);
            EventChannel.TransitionCreated.RemoveListener(OnTransition);
            EventChannel.ChangeScheduleRequested.RemoveListener(OnChangeScheduleRequested);
            EventChannel.ChangesScheduled.RemoveListener(OnChangesPlanned);
            EventChannel.ChangeApply.RemoveListener(OnChangeApply);
        }

        public override void Load(TileSet tileSet)
        {
            TilekitEventSource = new TilekitEventSource(EventChannel.Id, tileSet);

            base.Load(tileSet);
            
            EventChannel.TileSetLoaded.Invoke(TilekitEventSource, tileSet);
        }

        public void TriggerUpdate()
        {
            EventChannel.UpdateTriggered.Invoke(TilekitEventSource);
        }

        private void OnTileSetLoaded(TilekitEventSource source, TileSet tileSet)
        {
            Loaded.Invoke(tileSet);
        }

        protected virtual void OnFrustumChanged(TilekitEventSource tilekitEventSource, Plane[] planes)
        {
        }

        protected virtual void OnTilesSelected(TilekitEventSource tilekitEventSource, Tiles tiles)
        {
        }

        protected virtual void OnTransition(TilekitEventSource tilekitEventSource, List<Change> transition)
        {
        }

        protected virtual void OnChangeScheduleRequested(TilekitEventSource tilekitEventSource, Change change)
        {
        }

        protected virtual void OnChangesPlanned(TilekitEventSource tilekitEventSource, List<Change> changes)
        {
        }

        protected virtual void OnChangeApply(TilekitEventSource tilekitEventSource, Change change)
        {
            
        }
    }
}