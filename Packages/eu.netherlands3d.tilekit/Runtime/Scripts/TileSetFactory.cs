using UnityEngine;

namespace Netherlands3D.Tilekit
{
    public abstract class TileSetFactory : ScriptableObject
    {
        public abstract TileSet CreateTileSet();
    }
}