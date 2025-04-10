using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Tilekit.TileSets
{
    public abstract class TileMapper : MonoBehaviour
    {
        protected TileSet tileSet;
        
        // This is the result of the adding and removing of tiles, but may
        // be viewed separetely from tiles entering and exiting view
        // In practice, there will be a lot of overlap, but some TileMappers
        // may keep a cache of tiles; meaning entering and exiting view is
        // not the same from adding and removing
        //
        // Another example may be an explicitly tiled TileSet, all tiles will
        // be added immediately
        public UnityEvent<Tile> OnTileAdded = new();
        public UnityEvent<Tile> OnTileRemoved = new();

        // TODO: Is this a correct distinction?
        public UnityEvent<Tile> OnTileEntersView = new();
        public UnityEvent<Tile, Tile[]> OnTileReplacedWith = new();
        public UnityEvent<Tile> OnTileExitsView = new();

        public virtual void Load(TileSet tileSet)
        {
            this.tileSet = tileSet;
        }
        
        public virtual void Stage()
        {
        }

        public abstract void Render();
    }
}