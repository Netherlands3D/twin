using Netherlands3D.Tilekit.TileSets;
using UnityEngine;

namespace Netherlands3D.Twin.Tilekit
{
    public class TileGameObject : MonoBehaviour
    {
        public Tile Tile;
        
        private void Start()
        {
            // EventBus.TileSpawned(this);
            // We should subscribe to the service bus for loaded tile content - but how do we know whether
            // a TileContent started loading? 2 events? A start load and a finish loading?
        }
    }
}