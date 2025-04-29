using UnityEngine;

namespace Netherlands3D.Tilekit
{
    public abstract class BaseTileSetFactory : ScriptableObject
    {
        public abstract TileSet CreateTileSet();
    }
}