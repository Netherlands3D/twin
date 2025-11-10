using System;
using System.Collections.Generic;
using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Tilekit.TileSets;
using Netherlands3D.Twin.Tilekit;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Tilekit
{
    public abstract class BaseTileMapper : MonoBehaviour, ITilekitEvents, ITileMapper, ITileSetProvider
    {
        public string TileSetId { get; protected set; } = Guid.NewGuid().ToString();
        public TileSet? TileSet { get; protected set; }

        #region ITilekitEvents implementation
        public UnityEvent<ITileMapper> UpdateTriggered { get; } = new();
        public UnityEvent<ITileMapper, TileSet> TileSetLoaded { get; } = new();
        public UnityEvent<ITileMapper, Plane[]> FrustumChanged { get; } = new();
        public UnityEvent<ITileMapper, Tiles> TilesSelected { get; } = new();
        public UnityEvent<ITileMapper, List<Change>> TransitionCreated { get; } = new();
        public UnityEvent<ITileMapper, Change> ChangeScheduleRequested { get; } = new();
        public UnityEvent<ITileMapper, List<Change>> ChangesScheduled { get; } = new();
        public UnityEvent<ITileMapper, Change> ChangeApply { get; } = new();
        public UnityEvent<ITileMapper, TileBehaviour> TileSpawned { get; } = new();
        #endregion

        public void FromTileSet(TileSet tileSet)
        {
            // TODO: Initialize builder from existing TileSet
            throw new NotImplementedException();
        }

        protected virtual void OnEnable()
        {
            var eventSystem = TilekitEventSystem.current;
            if (!eventSystem) return;
            
            AddListeners(eventSystem);
        }

        protected virtual void OnDisable()
        {
            var eventSystem = TilekitEventSystem.current;
            if (!eventSystem) return;

            RemoveListeners(eventSystem);
        }

        private void AddListeners(ITilekitEvents events)
        {
            var eventSystem = TilekitEventSystem.current;
            if (!eventSystem) return;
            
            UpdateTriggered.AddListener(events.UpdateTriggered.Invoke);
            TileSetLoaded.AddListener(events.TileSetLoaded.Invoke);
            FrustumChanged.AddListener(events.FrustumChanged.Invoke);
            TilesSelected.AddListener(events.TilesSelected.Invoke);
            TransitionCreated.AddListener(events.TransitionCreated.Invoke);
            ChangeScheduleRequested.AddListener(events.ChangeScheduleRequested.Invoke);
            ChangesScheduled.AddListener(events.ChangesScheduled.Invoke);
            ChangeApply.AddListener(events.ChangeApply.Invoke);
            TileSpawned.AddListener(events.TileSpawned.Invoke);
        }

        private void RemoveListeners(ITilekitEvents events)
        {
            var eventSystem = TilekitEventSystem.current;
            if (!eventSystem) return;

            UpdateTriggered.RemoveListener(events.UpdateTriggered.Invoke);
            TileSetLoaded.RemoveListener(events.TileSetLoaded.Invoke);
            FrustumChanged.RemoveListener(events.FrustumChanged.Invoke);
            TilesSelected.RemoveListener(events.TilesSelected.Invoke);
            TransitionCreated.RemoveListener(events.TransitionCreated.Invoke);
            ChangeScheduleRequested.RemoveListener(events.ChangeScheduleRequested.Invoke);
            ChangesScheduled.RemoveListener(events.ChangesScheduled.Invoke);
            ChangeApply.RemoveListener(events.ChangeApply.Invoke);
            TileSpawned.RemoveListener(events.TileSpawned.Invoke);
        }
        
        public abstract void Map();
    }
}