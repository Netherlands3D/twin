using System.Collections.Generic;
using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Tilekit.TileSets;
using UnityEngine;

namespace Netherlands3D.Twin.Tilekit.Events
{
    public class EventChannel
    {
        public string Id { get; }

        public delegate void TickEvent(EventSource source);

        public delegate void FrustumChangedEvent(EventSource source, Plane[] frustumPlanes);

        public delegate void TilesSelectedEvent(EventSource source, Tiles tiles);

        public delegate void TransitionCreatedEvent(EventSource source, List<Change> transition);

        public delegate void ChangesPlannedEvent(EventSource source, List<Change> changes);

        public delegate void TileSpawnedEvent(EventSource source, TileGameObject tile);

        public TickEvent Tick;
        public FrustumChangedEvent FrustumChanged;
        public TilesSelectedEvent TilesSelected;
        public TransitionCreatedEvent TransitionCreated;
        public ChangesPlannedEvent ChangesPlanned;
        public TileSpawnedEvent TileSpawned;

        public EventChannel(string id)
        {
            Id = id;
        }

        public void Subscribe(EventChannel channel)
        {
            channel.Tick += Tick;
            channel.FrustumChanged += FrustumChanged;
            channel.TilesSelected += TilesSelected;
            channel.TransitionCreated += TransitionCreated;
            channel.ChangesPlanned += ChangesPlanned;
            channel.TileSpawned += TileSpawned;
        }

        public void Unsubscribe(EventChannel channel)
        {
            channel.Tick -= Tick;
            channel.FrustumChanged -= FrustumChanged;
            channel.TilesSelected -= TilesSelected;
            channel.TransitionCreated -= TransitionCreated;
            channel.ChangesPlanned -= ChangesPlanned;
            channel.TileSpawned -= TileSpawned;
        }
    }
}