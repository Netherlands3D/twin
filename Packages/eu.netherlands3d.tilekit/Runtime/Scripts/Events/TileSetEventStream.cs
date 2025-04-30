using System.Collections.Generic;
using Netherlands3D.Tilekit;
using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Tilekit.TileSets;
using UnityEngine;

namespace Netherlands3D.Twin.Tilekit.Events
{
    public class TileSetEventStream
    {
        public string EventStreamId { get; }

        public TileSetStreamEvent UpdateTriggered { get; } = new();
        public TileSetStreamEvent<TileSet> TileSetLoaded { get; } = new();
        public TileSetStreamEvent<Plane[]> FrustumChanged { get; } = new();
        public TileSetStreamEvent<Tiles> TilesSelected { get; } = new();
        public TileSetStreamEvent<List<Change>> TransitionCreated { get; } = new();
        public TileSetStreamEvent<Change> ChangeScheduleRequested { get; } = new();
        public TileSetStreamEvent<List<Change>> ChangesScheduled { get; } = new();
        public TileSetStreamEvent<Change> ChangeApply { get; } = new();
        public TileSetStreamEvent<TileBehaviour> TileSpawned { get; } = new();

        public TileSetEventStream(string eventStreamId)
        {
            EventStreamId = eventStreamId;
        }

        public void AddListener(TileSetEventStream stream)
        {
            stream.UpdateTriggered.AddListener(UpdateTriggered.Invoke);
            stream.TileSetLoaded.AddListener(TileSetLoaded.Invoke);
            stream.FrustumChanged.AddListener(FrustumChanged.Invoke);
            stream.TilesSelected.AddListener(TilesSelected.Invoke);
            stream.TransitionCreated.AddListener(TransitionCreated.Invoke);
            stream.ChangeScheduleRequested.AddListener(ChangeScheduleRequested.Invoke);
            stream.ChangesScheduled.AddListener(ChangesScheduled.Invoke);
            stream.ChangeApply.AddListener(ChangeApply.Invoke);
            stream.TileSpawned.AddListener(TileSpawned.Invoke);
        }

        public void RemoveListener(TileSetEventStream stream)
        {
            stream.UpdateTriggered.RemoveListener(UpdateTriggered.Invoke);
            stream.TileSetLoaded.RemoveListener(TileSetLoaded.Invoke);
            stream.FrustumChanged.RemoveListener(FrustumChanged.Invoke);
            stream.TilesSelected.RemoveListener(TilesSelected.Invoke);
            stream.TransitionCreated.RemoveListener(TransitionCreated.Invoke);
            stream.ChangeScheduleRequested.RemoveListener(ChangeScheduleRequested.Invoke);
            stream.ChangesScheduled.RemoveListener(ChangesScheduled.Invoke);
            stream.ChangeApply.RemoveListener(ChangeApply.Invoke);
            stream.TileSpawned.RemoveListener(TileSpawned.Invoke);
        }
    }
}