using System.Collections.Generic;
using Netherlands3D.Tilekit;
using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Tilekit.TileSets;
using RSG;
using UnityEngine;

namespace Netherlands3D.Twin.Tilekit.Events
{
    public class EventChannel
    {
        public string Id { get; }

        #region Delegate definitions
        public delegate void UpdateTriggeredEvent(EventSource source);
        public delegate void TileSetLoadedEvent(EventSource source, TileSet tileSet);
        public delegate void FrustumChangedEvent(EventSource source, Plane[] frustumPlanes);
        public delegate void TilesSelectedEvent(EventSource source, Tiles tiles);
        public delegate void TransitionCreatedEvent(EventSource source, List<Change> transition);
        public delegate void ChangeScheduleRequestedEvent(EventSource source, Change change);
        public delegate void ChangesScheduledEvent(EventSource source, List<Change> changes);
        public delegate Promise ChangeApplyEvent(EventSource source, Change change);
        public delegate void TileSpawnedEvent(EventSource source, TileBehaviour tile);
        #endregion

        #region Events
        public event UpdateTriggeredEvent UpdateTriggered;
        public event TileSetLoadedEvent TileSetLoaded;
        public event FrustumChangedEvent FrustumChanged;
        public event TilesSelectedEvent TilesSelected;
        public event TransitionCreatedEvent TransitionCreated;
        public event ChangeScheduleRequestedEvent ChangeScheduleRequested;
        public event ChangesScheduledEvent ChangesScheduled;
        public event ChangeApplyEvent ChangeApply;
        public event TileSpawnedEvent TileSpawned;
        #endregion

        public EventChannel(string id)
        {
            Id = id;
        }

        #region Named methods to use as Method Groups for binding (See Subscribe/Unsubscribe
        public void RaiseTriggerUpdated(EventSource source) => UpdateTriggered?.Invoke(source);
        public void RaiseTileSetLoaded(EventSource source, TileSet tileSet) => TileSetLoaded?.Invoke(source, tileSet);
        public void RaiseFrustumChanged(EventSource source, Plane[] frustumPlanes) => FrustumChanged?.Invoke(source, frustumPlanes);
        public void RaiseTilesSelected(EventSource source, Tiles tiles) => TilesSelected?.Invoke(source, tiles);
        public void RaiseTransitionCreated(EventSource source, List<Change> transition) => TransitionCreated?.Invoke(source, transition);
        public void RaiseChangeScheduleRequested(EventSource source, Change change) => ChangeScheduleRequested?.Invoke(source, change);
        public void RaiseChangesScheduled(EventSource source, List<Change> changes) => ChangesScheduled?.Invoke(source, changes);
        public Promise RaiseChangeApply(EventSource source, Change change) =>
            ChangeApply != null ? ChangeApply.Invoke(source, change) : Promise.Resolved() as Promise;
        public void RaiseTileSpawned(EventSource source, TileBehaviour tile) => TileSpawned?.Invoke(source, tile);
        #endregion

        #region Subscription management on other channels
        public void Subscribe(EventChannel channel)
        {
            channel.UpdateTriggered += RaiseTriggerUpdated;
            channel.TileSetLoaded += RaiseTileSetLoaded;
            channel.FrustumChanged += RaiseFrustumChanged;
            channel.TilesSelected += RaiseTilesSelected;
            channel.TransitionCreated += RaiseTransitionCreated;
            channel.ChangeScheduleRequested += RaiseChangeScheduleRequested;
            channel.ChangesScheduled += RaiseChangesScheduled;
            channel.ChangeApply += RaiseChangeApply;
            channel.TileSpawned += RaiseTileSpawned;
        }

        public void Unsubscribe(EventChannel channel)
        {
            channel.UpdateTriggered -= RaiseTriggerUpdated;
            channel.TileSetLoaded -= RaiseTileSetLoaded;
            channel.FrustumChanged -= RaiseFrustumChanged;
            channel.TilesSelected -= RaiseTilesSelected;
            channel.TransitionCreated -= RaiseTransitionCreated;
            channel.ChangeScheduleRequested -= RaiseChangeScheduleRequested;
            channel.ChangesScheduled -= RaiseChangesScheduled;
            channel.ChangeApply -= RaiseChangeApply;
            channel.TileSpawned -= RaiseTileSpawned;
        }
        #endregion
    }
}