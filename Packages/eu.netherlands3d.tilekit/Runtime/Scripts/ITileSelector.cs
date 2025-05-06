using Netherlands3D.Tilekit.TileSets;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    public interface ITileSelector
    {
        public Tiles Select(TileSet tileSet, Plane[] frustum);
    }
}