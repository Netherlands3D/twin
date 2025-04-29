using Netherlands3D.Tilekit;
using Netherlands3D.Tilekit.TileSets;
using Netherlands3D.Twin.Tilekit.Events;
using UnityEngine;

namespace Netherlands3D.Twin.Tilekit
{
    public class TileBehaviour : MonoBehaviour
    {
        public TileSet TileSet { get; set; }
        public Tile Tile { get; set; }
        public TilekitEventChannel EventChannel { get; set; }

        private void Start()
        {
            EventChannel.TileSpawned.Invoke(new TilekitEventSource(EventChannel.Id, TileSet), this);
            // We should subscribe to the service bus for loaded tile content - but how do we know whether
            // a TileContent started loading? 2 events? A start load and a finish loading?
        }
    }
}