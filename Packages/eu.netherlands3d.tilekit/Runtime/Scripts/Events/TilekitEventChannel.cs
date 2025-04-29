using System.Collections.Generic;
using Netherlands3D.Tilekit;
using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Tilekit.TileSets;
using UnityEngine;

namespace Netherlands3D.Twin.Tilekit.Events
{
    public class TilekitEventChannel
    {
        public string Id { get; }

        public TilekitEvent UpdateTriggered { get; } = new();
        public TilekitEvent<TileSet> TileSetLoaded { get; } = new();
        public TilekitEvent<Plane[]> FrustumChanged { get; } = new();
        public TilekitEvent<Tiles> TilesSelected { get; } = new();
        public TilekitEvent<List<Change>> TransitionCreated { get; } = new();
        public TilekitEvent<Change> ChangeScheduleRequested { get; } = new();
        public TilekitEvent<List<Change>> ChangesScheduled { get; } = new();
        public TilekitEvent<Change> ChangeApply { get; } = new();
        public TilekitEvent<TileBehaviour> TileSpawned { get; } = new();

        public TilekitEventChannel(string id)
        {
            Id = id;
        }

        public void AddListener(TilekitEventChannel channel)
        {
            channel.UpdateTriggered.AddListener(UpdateTriggered.Invoke);
            channel.TileSetLoaded.AddListener(TileSetLoaded.Invoke);
            channel.FrustumChanged.AddListener(FrustumChanged.Invoke);
            channel.TilesSelected.AddListener(TilesSelected.Invoke);
            channel.TransitionCreated.AddListener(TransitionCreated.Invoke);
            channel.ChangeScheduleRequested.AddListener(ChangeScheduleRequested.Invoke);
            channel.ChangesScheduled.AddListener(ChangesScheduled.Invoke);
            channel.ChangeApply.AddListener(ChangeApply.Invoke);
            channel.TileSpawned.AddListener(TileSpawned.Invoke);
        }

        public void RemoveListener(TilekitEventChannel channel)
        {
            channel.UpdateTriggered.RemoveListener(UpdateTriggered.Invoke);
            channel.TileSetLoaded.RemoveListener(TileSetLoaded.Invoke);
            channel.FrustumChanged.RemoveListener(FrustumChanged.Invoke);
            channel.TilesSelected.RemoveListener(TilesSelected.Invoke);
            channel.TransitionCreated.RemoveListener(TransitionCreated.Invoke);
            channel.ChangeScheduleRequested.RemoveListener(ChangeScheduleRequested.Invoke);
            channel.ChangesScheduled.RemoveListener(ChangesScheduled.Invoke);
            channel.ChangeApply.RemoveListener(ChangeApply.Invoke);
            channel.TileSpawned.RemoveListener(TileSpawned.Invoke);
        }
    }
}