using Netherlands3D.Tilekit.TileSets;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    public abstract class BaseTileSelector : ScriptableObject
    {
        public abstract Tiles Select(TileSet tileSet, Plane[] frustum);
    }
}