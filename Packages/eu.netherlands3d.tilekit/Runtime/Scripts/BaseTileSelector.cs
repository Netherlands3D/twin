using Netherlands3D.Tilekit.TileSets;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    public abstract class BaseTileSelector : MonoBehaviour
    {
        public abstract Tiles Select(TileSet tileSet, Plane[] frustum);
    }
}